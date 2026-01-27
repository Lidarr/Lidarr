using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using NLog;
using NzbDrone.Common.Composition;
using NzbDrone.Common.Composition.Extensions;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Exceptions;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Options;
using NzbDrone.Common.Reflection;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore.Extensions;
using PostgresOptions = NzbDrone.Core.Datastore.PostgresOptions;

namespace NzbDrone.Host
{
    public static class Bootstrap
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(Bootstrap));

        public static void Start(string[] args, Action<IHostBuilder> trayCallback = null)
        {
            try
            {
                Logger.Info("Starting Lidarr - {0} - Version {1}",
                            Environment.ProcessPath,
                            Assembly.GetExecutingAssembly().GetName().Version);

                var startupContext = new StartupContext(args);

                LongPathSupport.Enable();
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                var appMode = GetApplicationMode(startupContext);
                var config = GetConfiguration(startupContext);

                if (appMode is not(ApplicationModes.Interactive or ApplicationModes.Service))
                {
                    RunUtilityMode(appMode, startupContext, config);
                    return;
                }

                RunHostUntilShutdown(args, startupContext, appMode, trayCallback);

                Logger.Info("Lidarr has shut down completely");
            }
            catch (InvalidConfigFileException ex)
            {
                throw new LidarrStartupException(ex);
            }
            catch (AccessDeniedConfigFileException ex)
            {
                throw new LidarrStartupException(ex);
            }
            catch (TerminateApplicationException ex)
            {
                Logger.Info(ex.Message);
                LogManager.Configuration = null;
            }

            // Make sure there are no lingering database connections
            GC.Collect();
            GC.WaitForPendingFinalizers();
            SQLiteConnection.ClearAllPools();
        }

        private static void RunUtilityMode(ApplicationModes appMode, StartupContext startupContext, IConfiguration config)
        {
            Logger.Debug("Utility mode: {0}", appMode);

            var assemblies = AssemblyLoader.LoadBaseAssemblies();

            new HostBuilder()
                .UseServiceProviderFactory(new DryIocServiceProviderFactory(new Container(rules => rules.WithNzbDroneRules())))
                .ConfigureContainer<IContainer>(c =>
                {
                    c.AutoAddServices(assemblies)
                        .AddNzbDroneLogger()
                        .AddDatabase()
                        .AddStartupContext(startupContext)
                        .Resolve<UtilityModeRouter>()
                        .Route(appMode);

                    if (config.GetValue(nameof(ConfigFileProvider.LogDbEnabled), true))
                    {
                        c.AddLogDatabase();
                    }
                    else
                    {
                        c.AddDummyLogDatabase();
                    }
                })
                .ConfigureServices(services =>
                {
                    services.Configure<PostgresOptions>(config.GetSection("Lidarr:Postgres"));
                    services.Configure<AppOptions>(config.GetSection("Lidarr:App"));
                    services.Configure<AuthOptions>(config.GetSection("Lidarr:Auth"));
                    services.Configure<ServerOptions>(config.GetSection("Lidarr:Server"));
                    services.Configure<LogOptions>(config.GetSection("Lidarr:Log"));
                    services.Configure<UpdateOptions>(config.GetSection("Lidarr:Update"));
                })
                .Build();
        }

        private static void RunHostUntilShutdown(string[] args, StartupContext startupContext, ApplicationModes appMode, Action<IHostBuilder> trayCallback)
        {
            Logger.Debug("Starting in {0} mode", trayCallback != null ? "Tray" : appMode.ToString());

            bool shouldRestart;
            do
            {
                var success = RunHost(args, startupContext, trayCallback, true, out var pluginRefs, out shouldRestart);

                if (!success)
                {
                    var unloadSuccess = PluginLoader.UnloadPlugins(pluginRefs);

                    if (unloadSuccess)
                    {
                        RunHost(args, startupContext, trayCallback, false, out _, out shouldRestart);
                    }
                }

                if (shouldRestart)
                {
                    Logger.Info("Application restart requested, reinitializing host");
                    PluginLoader.UnloadPlugins(pluginRefs);
                    NzbDroneLogger.ResetAllTargets(startupContext, false, true);
                    Thread.Sleep(1000);
                }
            }
            while (shouldRestart);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool RunHost(string[] args, StartupContext startupContext, Action<IHostBuilder> trayCallback, bool usePlugins, out List<WeakReference> pluginRefs, out bool shouldRestart)
        {
            shouldRestart = false;

            var builder = CreateConsoleHostBuilder(args, startupContext, usePlugins, out pluginRefs);
            trayCallback?.Invoke(builder);

            if (OsInfo.IsWindows && WindowsServiceHelpers.IsWindowsService())
            {
                builder.UseWindowsService();
            }

            try
            {
                using var host = builder.Build();
                shouldRestart = RunWithRestartCheck(host);
            }
            catch (Exception e)
            {
                if (usePlugins)
                {
                    Logger.Warn(e, "Error starting with plugins enabled");
                }

                return false;
            }

            return true;
        }

        public static IHostBuilder CreateConsoleHostBuilder(string[] args, StartupContext context, bool usePlugins, out List<WeakReference> pluginRef)
        {
            var config = GetConfiguration(context);

            var bindAddress = config.GetValue<string>($"Lidarr:Server:{nameof(ServerOptions.BindAddress)}") ?? config.GetValue(nameof(ConfigFileProvider.BindAddress), "*");
            var port = config.GetValue<int?>($"Lidarr:Server:{nameof(ServerOptions.Port)}") ?? config.GetValue(nameof(ConfigFileProvider.Port), 8686);
            var sslPort = config.GetValue<int?>($"Lidarr:Server:{nameof(ServerOptions.SslPort)}") ?? config.GetValue(nameof(ConfigFileProvider.SslPort), 6868);
            var enableSsl = config.GetValue<bool?>($"Lidarr:Server:{nameof(ServerOptions.EnableSsl)}") ?? config.GetValue(nameof(ConfigFileProvider.EnableSsl), false);
            var sslCertPath = config.GetValue<string>($"Lidarr:Server:{nameof(ServerOptions.SslCertPath)}") ?? config.GetValue<string>(nameof(ConfigFileProvider.SslCertPath));
            var sslCertPassword = config.GetValue<string>($"Lidarr:Server:{nameof(ServerOptions.SslCertPassword)}") ?? config.GetValue<string>(nameof(ConfigFileProvider.SslCertPassword));
            var logDbEnabled = config.GetValue<bool?>($"Lidarr:Log:{nameof(LogOptions.DbEnabled)}") ?? config.GetValue(nameof(ConfigFileProvider.LogDbEnabled), true);

            var urls = new List<string> { BuildUrl("http", bindAddress, port) };

            if (enableSsl && sslCertPath.IsNotNullOrWhiteSpace())
            {
                urls.Add(BuildUrl("https", bindAddress, sslPort));
            }

            var assemblies = AssemblyLoader.LoadBaseAssemblies();
            pluginRef = null;

            if (usePlugins)
            {
                var pluginPaths = new AppFolderInfo(context).GetPluginAssemblies().ToList();
                (var plugins, pluginRef) = PluginLoader.LoadPlugins(pluginPaths);

                var loadedPlugins = plugins.Where(x => x != null).ToList();
                assemblies.AddRange(loadedPlugins);
                ReflectionExtensions.SetCurrentAssemblies(loadedPlugins);
            }
            else
            {
                ReflectionExtensions.SetCurrentAssemblies(Enumerable.Empty<Assembly>());
            }

            return new HostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseServiceProviderFactory(new DryIocServiceProviderFactory(new Container(rules => rules.WithNzbDroneRules())))
                .ConfigureContainer<IContainer>(c =>
                {
                    c.AutoAddServices(assemblies)
                        .SetPluginStatus(usePlugins)
                        .AddNzbDroneLogger()
                        .AddDatabase()
                        .AddStartupContext(context);

                    if (logDbEnabled)
                    {
                        c.AddLogDatabase();
                    }
                    else
                    {
                        c.AddDummyLogDatabase();
                    }
                })
                .ConfigureServices(services =>
                {
                    services.Configure<PostgresOptions>(config.GetSection("Lidarr:Postgres"));
                    services.Configure<AppOptions>(config.GetSection("Lidarr:App"));
                    services.Configure<AuthOptions>(config.GetSection("Lidarr:Auth"));
                    services.Configure<ServerOptions>(config.GetSection("Lidarr:Server"));
                    services.Configure<LogOptions>(config.GetSection("Lidarr:Log"));
                    services.Configure<UpdateOptions>(config.GetSection("Lidarr:Update"));
                })
                .ConfigureWebHost(builder =>
                {
                    builder.UseConfiguration(config);
                    builder.UseUrls(urls.ToArray());
                    builder.UseKestrel(options =>
                    {
                        if (enableSsl && sslCertPath.IsNotNullOrWhiteSpace())
                        {
                            options.ConfigureHttpsDefaults(configureOptions =>
                            {
                                configureOptions.ServerCertificate = ValidateSslCertificate(sslCertPath, sslCertPassword);
                            });
                        }
                    });
                    builder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.AllowSynchronousIO = false;
                        serverOptions.Limits.MaxRequestBodySize = null;
                    });
                    builder.UseStartup<Startup>();
                });
        }

        private static ApplicationModes GetApplicationMode(IStartupContext startupContext)
        {
            if (startupContext.Help)
            {
                return ApplicationModes.Help;
            }

            if (OsInfo.IsWindows && startupContext.RegisterUrl)
            {
                return ApplicationModes.RegisterUrl;
            }

            if (OsInfo.IsWindows && startupContext.InstallService)
            {
                return ApplicationModes.InstallService;
            }

            if (OsInfo.IsWindows && startupContext.UninstallService)
            {
                return ApplicationModes.UninstallService;
            }

            // IsWindowsService can throw sometimes, so wrap it
            var isWindowsService = false;
            try
            {
                isWindowsService = WindowsServiceHelpers.IsWindowsService();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get service status");
            }

            if (OsInfo.IsWindows && isWindowsService)
            {
                return ApplicationModes.Service;
            }

            return ApplicationModes.Interactive;
        }

        private static IConfiguration GetConfiguration(StartupContext context)
        {
            var appFolder = new AppFolderInfo(context);
            var configPath = appFolder.GetConfigPath();

            try
            {
                return new ConfigurationBuilder()
                    .AddXmlFile(configPath, optional: true, reloadOnChange: false)
                    .AddInMemoryCollection(new List<KeyValuePair<string, string>> { new("dataProtectionFolder", appFolder.GetDataProtectionPath()) })
                    .AddEnvironmentVariables()
                    .Build();
            }
            catch (InvalidDataException ex)
            {
                Logger.Error(ex, ex.Message);

                throw new InvalidConfigFileException($"{configPath} is corrupt or invalid. Please delete the config file and Lidarr will recreate it.", ex);
            }
        }

        private static string BuildUrl(string scheme, string bindAddress, int port)
        {
            return $"{scheme}://{bindAddress}:{port}";
        }

        private static X509Certificate2 ValidateSslCertificate(string cert, string password)
        {
            X509Certificate2 certificate;

            try
            {
                certificate = new X509Certificate2(cert, password, X509KeyStorageFlags.DefaultKeySet);
            }
            catch (CryptographicException ex)
            {
                if (ex.HResult == 0x2 || ex.HResult == 0x2006D080)
                {
                    throw new LidarrStartupException(ex,
                        $"The SSL certificate file {cert} does not exist");
                }

                throw new LidarrStartupException(ex);
            }

            return certificate;
        }

        private static bool RunWithRestartCheck(IHost host)
        {
            var shouldRestart = false;

            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopped.Register(() =>
            {
                var runtimeInfo = host.Services.GetRequiredService<IRuntimeInfo>();
                shouldRestart = runtimeInfo.RestartPending;
            });

            host.Run();
            return shouldRestart;
        }
    }
}
