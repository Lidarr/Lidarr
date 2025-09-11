using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Connections;
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

                switch (appMode)
                {
                    case ApplicationModes.Service:
                        StartService(startupContext);
                        break;
                    case ApplicationModes.Interactive:
                        StartInteractive(startupContext, trayCallback);
                        break;
                    default:
                        StartUtility(startupContext, appMode, config);
                        break;
                }
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StartService(StartupContext context)
        {
            Logger.Info("BOOTSTRAP Service mode selected");
            Console.WriteLine("BOOTSTRAP Service mode selected");

            try
            {
                var success = StartService(context, true, out var pluginRefs);

                if (!success)
                {
                    Logger.Info("BOOTSTRAP Service startup with plugins failed, checking if retry is appropriate");
                    Console.WriteLine("BOOTSTRAP Service startup with plugins failed, checking if retry is appropriate");

                    var unloadSuccess = PluginLoader.UnloadPlugins(pluginRefs);

                    if (unloadSuccess)
                    {
                        Logger.Info("BOOTSTRAP Plugins unloaded successfully, attempting service startup without plugins");
                        Console.WriteLine("BOOTSTRAP Plugins unloaded successfully, attempting service startup without plugins");
                        StartService(context, false, out _);
                    }
                    else
                    {
                        Logger.Info("BOOTSTRAP Plugin unload failed, skipping retry to prevent issues");
                        Console.WriteLine("BOOTSTRAP Plugin unload failed, skipping retry to prevent issues");
                    }
                }
            }
            catch (Exception e) when (IsPortBindingError(e))
            {
                Logger.Info("BOOTSTRAP Port binding error detected in service startup, skipping retry to prevent restart loop");
                Console.WriteLine("BOOTSTRAP Port binding error detected in service startup, skipping retry to prevent restart loop");
                Logger.Info("BOOTSTRAP Port binding exception type: {0} message: {1}", e.GetType().Name, e.Message);
                Console.WriteLine("BOOTSTRAP Port binding exception type: {0} message: {1}", e.GetType().Name, e.Message);
                return;
            }

            Logger.Info("BOOTSTRAP Creating final service host builder without plugins");
            Console.WriteLine("BOOTSTRAP Creating final service host builder without plugins");
            CreateConsoleHostBuilder(context, false, out _).UseWindowsService().Build().Run();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StartInteractive(StartupContext context, Action<IHostBuilder> trayCallback)
        {
            Logger.Info("BOOTSTRAP Interactive mode selected with tray callback {0}", trayCallback != null ? "enabled" : "disabled");
            Console.WriteLine("BOOTSTRAP Interactive mode selected with tray callback {0}", trayCallback != null ? "enabled" : "disabled");

            try
            {
                var success = StartInteractive(context, trayCallback, true, out var pluginRefs);

                if (!success)
                {
                    Logger.Info("BOOTSTRAP Interactive startup with plugins failed, checking if retry is appropriate");
                    Console.WriteLine("BOOTSTRAP Interactive startup with plugins failed, checking if retry is appropriate");

                    var unloadSuccess = PluginLoader.UnloadPlugins(pluginRefs);

                    if (unloadSuccess)
                    {
                        Logger.Info("BOOTSTRAP Plugins unloaded successfully, attempting interactive startup without plugins");
                        Console.WriteLine("BOOTSTRAP Plugins unloaded successfully, attempting interactive startup without plugins");
                        StartInteractive(context, trayCallback, false, out _);
                    }
                    else
                    {
                        Logger.Info("BOOTSTRAP Plugin unload failed, skipping retry to prevent issues");
                        Console.WriteLine("BOOTSTRAP Plugin unload failed, skipping retry to prevent issues");
                    }
                }
            }
            catch (Exception e) when (IsPortBindingError(e))
            {
                Logger.Info("BOOTSTRAP Port binding error detected in interactive startup, skipping retry to prevent restart loop");
                Console.WriteLine("BOOTSTRAP Port binding error detected in interactive startup, skipping retry to prevent restart loop");
                Logger.Info("BOOTSTRAP Port binding exception type: {0} message: {1}", e.GetType().Name, e.Message);
                Console.WriteLine("BOOTSTRAP Port binding exception type: {0} message: {1}", e.GetType().Name, e.Message);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool StartService(StartupContext context, bool usePlugins, out List<WeakReference> pluginRefs)
        {
            var builder = CreateConsoleHostBuilder(context, usePlugins, out pluginRefs).UseWindowsService();

            return RunBuilder(builder, usePlugins);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool StartInteractive(StartupContext context, Action<IHostBuilder> trayCallback, bool usePlugins, out List<WeakReference> pluginRefs)
        {
            var builder = CreateConsoleHostBuilder(context, usePlugins, out pluginRefs);

            if (trayCallback != null)
            {
                trayCallback(builder);
            }

            return RunBuilder(builder, usePlugins);
        }

        private static bool RunBuilder(IHostBuilder builder, bool usePlugins)
        {
            try
            {
                Logger.Info("BOOTSTRAP Building host with plugins enabled {0}", usePlugins);
                Console.WriteLine("BOOTSTRAP Building host with plugins enabled {0}", usePlugins);
                using var host = builder.Build();
                Logger.Info("BOOTSTRAP Host built successfully, starting host execution");
                Console.WriteLine("BOOTSTRAP Host built successfully, starting host execution");
                host.Run();
                Logger.Info("BOOTSTRAP Host execution completed normally");
                Console.WriteLine("BOOTSTRAP Host execution completed normally");
            }
            catch (Exception e) when (IsPortBindingError(e))
            {
                Logger.Info("BOOTSTRAP Port binding error detected, re-throwing to prevent retry loop");
                Console.WriteLine("BOOTSTRAP Port binding error detected, re-throwing to prevent retry loop");
                throw;
            }
            catch (Exception e)
            {
                if (usePlugins)
                {
                    Logger.Info("BOOTSTRAP Error starting with plugins enabled: {0}", e.Message);
                    Console.WriteLine("BOOTSTRAP Error starting with plugins enabled: {0}", e.Message);
                }
                else
                {
                    Logger.Info("BOOTSTRAP Error starting without plugins, this is likely a configuration or system issue: {0}", e.Message);
                    Console.WriteLine("BOOTSTRAP Error starting without plugins, this is likely a configuration or system issue: {0}", e.Message);
                }

                Logger.Info("BOOTSTRAP Returning false to indicate startup failure");
                Console.WriteLine("BOOTSTRAP Returning false to indicate startup failure");
                return false;
            }

            Logger.Info("BOOTSTRAP Returning true to indicate successful startup");
            Console.WriteLine("BOOTSTRAP Returning true to indicate successful startup");
            return true;
        }

        private static bool IsPortBindingError(Exception e)
        {
            Logger.Info("BOOTSTRAP Checking exception type {0} for port binding error patterns", e.GetType().Name);
            Console.WriteLine("BOOTSTRAP Checking exception type {0} for port binding error patterns", e.GetType().Name);

            // Check for direct AddressInUseException
            if (e is AddressInUseException)
            {
                Logger.Info("BOOTSTRAP Port binding error detected: Direct AddressInUseException");
                Console.WriteLine("BOOTSTRAP Port binding error detected: Direct AddressInUseException");
                return true;
            }

            // Check for SocketException with AddressAlreadyInUse error code
            if (e is SocketException socketEx)
            {
                Logger.Info("BOOTSTRAP Found SocketException with error code {0}", socketEx.SocketErrorCode);
                Console.WriteLine("BOOTSTRAP Found SocketException with error code {0}", socketEx.SocketErrorCode);

                if (socketEx.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    Logger.Info("BOOTSTRAP Port binding error detected: SocketException with AddressAlreadyInUse error code");
                    Console.WriteLine("BOOTSTRAP Port binding error detected: SocketException with AddressAlreadyInUse error code");
                    return true;
                }
            }

            // Check for IOException wrapping AddressInUseException
            if (e is IOException ioEx)
            {
                Logger.Info("BOOTSTRAP Found IOException, checking inner exception type {0}", ioEx.InnerException?.GetType().Name ?? "null");
                Console.WriteLine("BOOTSTRAP Found IOException, checking inner exception type {0}", ioEx.InnerException?.GetType().Name ?? "null");

                if (ioEx.InnerException is AddressInUseException)
                {
                    Logger.Info("BOOTSTRAP Port binding error detected: IOException wrapping AddressInUseException");
                    Console.WriteLine("BOOTSTRAP Port binding error detected: IOException wrapping AddressInUseException");
                    return true;
                }

                // Check for IOException wrapping SocketException with AddressAlreadyInUse
                if (ioEx.InnerException is SocketException innerSocketEx)
                {
                    Logger.Info("BOOTSTRAP Found IOException wrapping SocketException with error code {0}", innerSocketEx.SocketErrorCode);
                    Console.WriteLine("BOOTSTRAP Found IOException wrapping SocketException with error code {0}", innerSocketEx.SocketErrorCode);

                    if (innerSocketEx.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    {
                        Logger.Info("BOOTSTRAP Port binding error detected: IOException wrapping SocketException with AddressAlreadyInUse error code");
                        Console.WriteLine("BOOTSTRAP Port binding error detected: IOException wrapping SocketException with AddressAlreadyInUse error code");
                        return true;
                    }
                }
            }

            Logger.Info("BOOTSTRAP No port binding error pattern detected for exception type {0}", e.GetType().Name);
            Console.WriteLine("BOOTSTRAP No port binding error pattern detected for exception type {0}", e.GetType().Name);
            return false;
        }

        private static void StartUtility(StartupContext context, ApplicationModes mode, IConfiguration config)
        {
            var assemblies = AssemblyLoader.LoadBaseAssemblies();
            new HostBuilder()
                .UseServiceProviderFactory(new DryIocServiceProviderFactory(new Container(rules => rules.WithNzbDroneRules())))
                .ConfigureContainer<IContainer>(c =>
                {
                    c.AutoAddServices(assemblies)
                        .AddNzbDroneLogger()
                        .AddDatabase()
                        .AddStartupContext(context)
                        .Resolve<UtilityModeRouter>()
                        .Route(mode);

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
                }).Build();
        }

        private static IHostBuilder CreateConsoleHostBuilder(StartupContext context, bool usePlugins, out List<WeakReference> pluginRef)
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

                assemblies.AddRange(plugins.Where(x => x != null));
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
                    .AddInMemoryCollection(new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("dataProtectionFolder", appFolder.GetDataProtectionPath()) })
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
    }
}
