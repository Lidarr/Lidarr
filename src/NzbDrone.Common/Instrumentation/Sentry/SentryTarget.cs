using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Data.SQLite;
using NLog;
using NLog.Common;
using NLog.Targets;
using NzbDrone.Common.EnvironmentInfo;
using SharpRaven;
using SharpRaven.Data;
using System.Globalization;

namespace NzbDrone.Common.Instrumentation.Sentry
{
    [Target("Sentry")]
    public class SentryTarget : TargetWithLayout
    {
        private readonly RavenClient _client;

        private static readonly IDictionary<LogLevel, ErrorLevel> LoggingLevelMap = new Dictionary<LogLevel, ErrorLevel>
        {
            {LogLevel.Debug, ErrorLevel.Debug},
            {LogLevel.Error, ErrorLevel.Error},
            {LogLevel.Fatal, ErrorLevel.Fatal},
            {LogLevel.Info, ErrorLevel.Info},
            {LogLevel.Trace, ErrorLevel.Debug},
            {LogLevel.Warn, ErrorLevel.Warning},
        };

        private readonly SentryDebounce _debounce;
        private bool _unauthorized;


        public SentryTarget(string dsn)
        {
            _client = new RavenClient(new Dsn(dsn), new LidarrJsonPacketFactory(), new SentryRequestFactory(), new MachineNameUserFactory())
            {
                Compression = true,
                Environment = RuntimeInfo.IsProduction ? "production" : "development",
                Release = BuildInfo.Release,
                ErrorOnCapture = OnError
            };


            _client.Tags.Add("osfamily", OsInfo.Os.ToString());
            _client.Tags.Add("runtime", PlatformInfo.PlatformName);
            _client.Tags.Add("culture", Thread.CurrentThread.CurrentCulture.Name);
            _client.Tags.Add("branch", BuildInfo.Branch);
            _client.Tags.Add("version", BuildInfo.Version.ToString());

            _debounce = new SentryDebounce();
        }

        private void OnError(Exception ex)
        {
            var webException = ex as WebException;

            if (webException != null)
            {
                var response = webException.Response as HttpWebResponse;
                var statusCode = response?.StatusCode;
                if (statusCode == HttpStatusCode.Unauthorized)
                {
                    _unauthorized = true;
                    _debounce.Clear();
                }
            }

            InternalLogger.Error(ex, "Unable to send error to Sentry");
        }

        private static List<string> GetFingerPrint(LogEventInfo logEvent)
        {
            if (logEvent.Properties.ContainsKey("Sentry"))
            {
                return ((string[])logEvent.Properties["Sentry"]).ToList();
            }

            var fingerPrint = new List<string>
            {
                logEvent.Level.Ordinal.ToString(),
                logEvent.LoggerName
            };

            var ex = logEvent.Exception;

            if (ex != null)
            {
                var exception = ex.GetType().Name;

                if (ex.InnerException != null)
                {
                    exception += ex.InnerException.GetType().Name;
                }

                fingerPrint.Add(exception);
            }

            return fingerPrint;
        }

        private bool IsSentryMessage(LogEventInfo logEvent)
        {
            if (logEvent.Properties.ContainsKey("Sentry"))
            {
                return logEvent.Properties["Sentry"] != null;
            }

            if (logEvent.Level >= LogLevel.Error && logEvent.Exception != null)
            {
                // don't report uninformative SQLite exceptions
                // busy/locked are benign https://forums.sonarr.tv/t/owin-sqlite-error-5-database-is-locked/5423/11
                // The others will be user configuration problems and silt up Sentry
                var sqlEx = logEvent.Exception as SQLiteException;
                if (sqlEx != null && (sqlEx.ResultCode == SQLiteErrorCode.Busy ||
                                      sqlEx.ResultCode == SQLiteErrorCode.Locked ||
                                      sqlEx.ResultCode == SQLiteErrorCode.Perm ||
                                      sqlEx.ResultCode == SQLiteErrorCode.ReadOnly ||
                                      sqlEx.ResultCode == SQLiteErrorCode.IoErr ||
                                      sqlEx.ResultCode == SQLiteErrorCode.Corrupt ||
                                      sqlEx.ResultCode == SQLiteErrorCode.Full ||
                                      sqlEx.ResultCode == SQLiteErrorCode.CantOpen ||
                                      sqlEx.ResultCode == SQLiteErrorCode.Auth))
                {
                    return false;
                }

                // Swallow the many, many exceptions flowing through from Jackett
                if (logEvent.Exception.Message.Contains("Jackett.Common.IndexerException"))
                {
                    return false;
                }

                // UnauthorizedAccessExceptions will just be user configuration issues
                if (logEvent.Exception is UnauthorizedAccessException)
                {
                    return false;
                }

                return true;
            }

            return false;
        }


        protected override void Write(LogEventInfo logEvent)
        {
            if (_unauthorized)
            {
                return;
            }

            try
            {
                // don't report non-critical events without exceptions
                if (!IsSentryMessage(logEvent))
                {
                    return;
                }

                var fingerPrint = GetFingerPrint(logEvent);
                if (!_debounce.Allowed(fingerPrint))
                {
                    return;
                }

                var extras = logEvent.Properties.ToDictionary(x => x.Key.ToString(), x => x.Value.ToString());
                extras.Remove("Sentry");
                _client.Logger = logEvent.LoggerName;

                if (logEvent.Exception != null)
                {
                    foreach (DictionaryEntry data in logEvent.Exception.Data)
                    {
                        extras.Add(data.Key.ToString(), data.Value.ToString());
                    }
                }

                var sentryMessage = new SentryMessage(logEvent.Message, logEvent.Parameters);

                var sentryEvent = new SentryEvent(logEvent.Exception)
                {
                    Level = LoggingLevelMap[logEvent.Level],
                    Message = sentryMessage,
                    Extra = extras,
                    Fingerprint =
                    {
                        logEvent.Level.ToString(),
                        logEvent.LoggerName,
                        logEvent.Message
                    }
                };

                // Fix openflixr being stupid with permissions
                var serverName = sentryEvent.Contexts.Device.Name.ToLower();

                if (serverName == "openflixr")
                {
                    return;
                }

                // bodge to try to get the exception message in English
                // https://stackoverflow.com/questions/209133/exception-messages-in-english
                // There may still be some localization but this is better than nothing.
                string message = string.Empty;

                // only try to use the exeception message to fingerprint if there's no inner
                // exception and the message is short, otherwise we're in danger of getting a
                // stacktrace which will break the grouping
                if (logEvent.Exception != null && logEvent.Exception.InnerException == null)
                {
                    var t = new Thread(() => {
                            message = logEvent.Exception?.Message;
                        });
                    t.CurrentCulture = CultureInfo.InvariantCulture;
                    t.CurrentUICulture = CultureInfo.InvariantCulture;
                    t.Start();
                    t.Join();
                }

                Console.WriteLine($"Sentry fingerprint message {message}");

                if (logEvent.Exception != null)
                {
                    sentryEvent.Fingerprint.Add(logEvent.Exception.GetType().FullName);
                    sentryEvent.Fingerprint.Add(logEvent.Exception.TargetSite.ToString());
                    sentryEvent.Fingerprint.Add(message.Length < 200 ? message : string.Empty);
                }

                if (logEvent.Properties.ContainsKey("Sentry"))
                {
                    sentryEvent.Fingerprint.Clear();
                    Array.ForEach((string[])logEvent.Properties["Sentry"], sentryEvent.Fingerprint.Add);
                }

                var osName = Environment.GetEnvironmentVariable("OS_NAME");
                var osVersion = Environment.GetEnvironmentVariable("OS_VERSION");
                var runTimeVersion = Environment.GetEnvironmentVariable("RUNTIME_VERSION");

                sentryEvent.Tags.Add("os_name", osName);
                sentryEvent.Tags.Add("os_version", $"{osName} {osVersion}");
                sentryEvent.Tags.Add("runtime_version", $"{PlatformInfo.PlatformName} {runTimeVersion}");

                _client.Capture(sentryEvent);
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }
    }
}
