using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Host
{
    public class AppLifetime : IHostedService, IHandle<ApplicationShutdownRequested>
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IRuntimeInfo _runtimeInfo;
        private readonly IStartupContext _startupContext;
        private readonly IBrowserService _browserService;
        private readonly IProcessProvider _processProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public AppLifetime(IHostApplicationLifetime appLifetime,
            IConfigFileProvider configFileProvider,
            IRuntimeInfo runtimeInfo,
            IStartupContext startupContext,
            IBrowserService browserService,
            IProcessProvider processProvider,
            IEventAggregator eventAggregator,
            Logger logger)
        {
            _appLifetime = appLifetime;
            _configFileProvider = configFileProvider;
            _runtimeInfo = runtimeInfo;
            _startupContext = startupContext;
            _browserService = browserService;
            _processProvider = processProvider;
            _eventAggregator = eventAggregator;
            _logger = logger;

            appLifetime.ApplicationStarted.Register(OnAppStarted);
            appLifetime.ApplicationStopped.Register(OnAppStopped);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info("AppLifetime StartAsync called with cancellation requested {0}", cancellationToken.IsCancellationRequested);
            Console.WriteLine("AppLifetime StartAsync called with cancellation requested {0}", cancellationToken.IsCancellationRequested);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnAppStarted()
        {
            _logger.Info("OnAppStarted called for Process ID {0}", Environment.ProcessId);
            Console.WriteLine("OnAppStarted called for Process ID {0}", Environment.ProcessId);
            _runtimeInfo.IsStarting = false;
            _runtimeInfo.IsExiting = false;

            if (!_startupContext.Flags.Contains(StartupContext.NO_BROWSER)
                && _configFileProvider.LaunchBrowser)
            {
                _browserService.LaunchWebUI();
            }

            _eventAggregator.PublishEvent(new ApplicationStartedEvent());
        }

        private void OnAppStopped()
        {
            if (_runtimeInfo.RestartPending && !_runtimeInfo.IsWindowsService)
            {
                _logger.Info("Restart pending detected, evaluating restart method");
                Console.WriteLine("Restart pending detected, evaluating restart method");
                _logger.Info("Runtime environment IsWindowsService {0} IsSystemdService {1} IsContainerized {2}", _runtimeInfo.IsWindowsService, _runtimeInfo.IsSystemdService, _runtimeInfo.IsContainerized);
                Console.WriteLine("Runtime environment IsWindowsService {0} IsSystemdService {1} IsContainerized {2}", _runtimeInfo.IsWindowsService, _runtimeInfo.IsSystemdService, _runtimeInfo.IsContainerized);

                if (_runtimeInfo.IsSystemdService || _runtimeInfo.IsContainerized)
                {
                    if (_runtimeInfo.IsSystemdService && _runtimeInfo.IsContainerized)
                    {
                        _logger.Info("Restart handled by systemd AND container, letting external process manager handle restart");
                        Console.WriteLine("Restart handled by systemd AND container, letting external process manager handle restart");
                    }
                    else if (_runtimeInfo.IsSystemdService)
                    {
                        _logger.Info("Restart handled by systemd, letting systemd handle restart");
                        Console.WriteLine("Restart handled by systemd, letting systemd handle restart");
                    }
                    else
                    {
                        _logger.Info("Restart handled by container, letting container runtime handle restart");
                        Console.WriteLine("Restart handled by container, letting container runtime handle restart");
                    }

                    _logger.Info("Skipping process spawn to prevent double restart");
                    Console.WriteLine("Skipping process spawn to prevent double restart");
                    return;
                }

                _logger.Info("Manual restart required, spawning new process");
                Console.WriteLine("Manual restart required, spawning new process");
                var restartArgs = GetRestartArgs();
                _logger.Info("Attempting restart with arguments: {0}", restartArgs);
                Console.WriteLine("Attempting restart with arguments: {0}", restartArgs);
                _logger.Info("Spawning: {0} {1}", _runtimeInfo.ExecutingApplication, restartArgs);
                Console.WriteLine("Spawning: {0} {1}", _runtimeInfo.ExecutingApplication, restartArgs);

                _processProvider.SpawnNewProcess(_runtimeInfo.ExecutingApplication, restartArgs);
                LogOtherInstances();
                _logger.Info("New process spawned successfully");
                Console.WriteLine("New process spawned successfully");
            }
        }

        private void LogOtherInstances()
        {
            try
            {
                var currentId = _processProvider.GetCurrentProcess().Id;
                var otherProcesses = _processProvider.FindProcessByName(ProcessProvider.LIDARR_CONSOLE_PROCESS_NAME)
                                                     .Union(_processProvider.FindProcessByName(ProcessProvider.LIDARR_PROCESS_NAME))
                                                     .Where(p => p.Id != currentId)
                                                     .ToList();

                _logger.Info("RESTART_SEQUENCE Found {0} other Lidarr processes running", otherProcesses.Count);
                Console.WriteLine("RESTART_SEQUENCE Found {0} other Lidarr processes running", otherProcesses.Count);

                foreach (var process in otherProcesses)
                {
                    _logger.Info("RESTART_SEQUENCE Other process found ID {0} Name {1}", process.Id, process.Name);
                    Console.WriteLine("RESTART_SEQUENCE Other process found ID {0} Name {1}", process.Id, process.Name);
                }

                if (otherProcesses.Any())
                {
                    _logger.Info("RESTART_SEQUENCE Warning other Lidarr instances detected, may cause port conflicts");
                    Console.WriteLine("RESTART_SEQUENCE Warning other Lidarr instances detected, may cause port conflicts");
                }
            }
            catch (Exception ex)
            {
                _logger.Info("RESTART_SEQUENCE Failed to check for other instances: {0}", ex.Message);
                Console.WriteLine("RESTART_SEQUENCE Failed to check for other instances: {0}", ex.Message);
            }
        }

        private void Shutdown()
        {
            _logger.Info("Attempting to stop application");
            Console.WriteLine("Attempting to stop application");
            _logger.Info("Application has finished stop routine");
            Console.WriteLine("Application has finished stop routine");
            _runtimeInfo.IsExiting = true;
            _appLifetime.StopApplication();
        }

        private string GetRestartArgs()
        {
            var args = _startupContext.PreservedArguments;

            args += " /restart";

            if (!args.Contains("/nobrowser"))
            {
                args += " /nobrowser";
            }

            return args;
        }

        [EventHandleOrder(EventHandleOrder.Last)]
        public void Handle(ApplicationShutdownRequested message)
        {
            if (!_runtimeInfo.IsWindowsService)
            {
                if (message.Restarting)
                {
                    _runtimeInfo.RestartPending = true;
                    _logger.Info("Restart flag set to true");
                    Console.WriteLine("Restart flag set to true");
                }

                _logger.Info("Runtime environment IsWindowsService {0} IsSystemdService {1} IsContainerized {2}", _runtimeInfo.IsWindowsService, _runtimeInfo.IsSystemdService, _runtimeInfo.IsContainerized);
                Console.WriteLine("Runtime environment IsWindowsService {0} IsSystemdService {1} IsContainerized {2}", _runtimeInfo.IsWindowsService, _runtimeInfo.IsSystemdService, _runtimeInfo.IsContainerized);

                if (_runtimeInfo.IsSystemdService || _runtimeInfo.IsContainerized)
                {
                    if (_runtimeInfo.IsSystemdService && _runtimeInfo.IsContainerized)
                    {
                        _logger.Info("Restart handled by systemd AND container, letting external process manager handle restart");
                        Console.WriteLine("Restart handled by systemd AND container, letting external process manager handle restart");
                    }
                    else if (_runtimeInfo.IsSystemdService)
                    {
                        _logger.Info("Restart handled by systemd, letting systemd handle restart");
                        Console.WriteLine("Restart handled by systemd, letting systemd handle restart");
                    }
                    else
                    {
                        _logger.Info("Restart handled by container, letting container runtime handle restart");
                        Console.WriteLine("Restart handled by container, letting container runtime handle restart");
                    }

                    _logger.Info("Skipping process spawn to prevent double restart");
                    Console.WriteLine("Skipping process spawn to prevent double restart");
                }

                LogManager.Configuration = null;
                LogOtherInstances();
                Shutdown();
            }
        }
    }
}
