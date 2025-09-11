using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Processes;

namespace NzbDrone.Host
{
    public interface ISingleInstancePolicy
    {
        void PreventStartIfAlreadyRunning();
        void KillAllOtherInstance();
        void WarnIfAlreadyRunning();
    }

    public class SingleInstancePolicy : ISingleInstancePolicy
    {
        private readonly IProcessProvider _processProvider;
        private readonly IBrowserService _browserService;
        private readonly Logger _logger;

        public SingleInstancePolicy(IProcessProvider processProvider,
                                    IBrowserService browserService,
                                    Logger logger)
        {
            _processProvider = processProvider;
            _browserService = browserService;
            _logger = logger;
        }

        public void PreventStartIfAlreadyRunning()
        {
            _logger.Info("INSTANCE_CHECK Starting single instance prevention check");
            Console.WriteLine("INSTANCE_CHECK Starting single instance prevention check");

            if (IsAlreadyRunning())
            {
                _logger.Info("INSTANCE_CHECK Another instance of Lidarr is already running, preventing startup");
                Console.WriteLine("INSTANCE_CHECK Another instance of Lidarr is already running, preventing startup");
                _browserService.LaunchWebUI();
                throw new TerminateApplicationException("Another instance is already running");
            }

            _logger.Info("INSTANCE_CHECK No other instances found, startup allowed");
            Console.WriteLine("INSTANCE_CHECK No other instances found, startup allowed");
        }

        public void KillAllOtherInstance()
        {
            _logger.Info("INSTANCE_KILL Starting kill all other instances process");
            Console.WriteLine("INSTANCE_KILL Starting kill all other instances process");

            var otherInstances = GetOtherNzbDroneProcessIds();
            _logger.Info("INSTANCE_KILL Found {0} other instances to terminate", otherInstances.Count);
            Console.WriteLine("INSTANCE_KILL Found {0} other instances to terminate", otherInstances.Count);

            foreach (var processId in otherInstances)
            {
                _logger.Info("INSTANCE_KILL Terminating process ID {0}", processId);
                Console.WriteLine("INSTANCE_KILL Terminating process ID {0}", processId);
                _processProvider.Kill(processId);
            }

            _logger.Info("INSTANCE_KILL Completed termination of other instances");
            Console.WriteLine("INSTANCE_KILL Completed termination of other instances");
        }

        public void WarnIfAlreadyRunning()
        {
            _logger.Info("INSTANCE_WARN Starting already running warning check");
            Console.WriteLine("INSTANCE_WARN Starting already running warning check");

            if (IsAlreadyRunning())
            {
                _logger.Info("INSTANCE_WARN Another instance of Lidarr is already running");
                Console.WriteLine("INSTANCE_WARN Another instance of Lidarr is already running");
            }
            else
            {
                _logger.Info("INSTANCE_WARN No other instances detected");
                Console.WriteLine("INSTANCE_WARN No other instances detected");
            }
        }

        private bool IsAlreadyRunning()
        {
            return GetOtherNzbDroneProcessIds().Any();
        }

        private List<int> GetOtherNzbDroneProcessIds()
        {
            try
            {
                var currentId = _processProvider.GetCurrentProcess().Id;
                _logger.Info("INSTANCE_DETECT Current process ID is {0}", currentId);
                Console.WriteLine("INSTANCE_DETECT Current process ID is {0}", currentId);

                var consoleProcesses = _processProvider.FindProcessByName(ProcessProvider.LIDARR_CONSOLE_PROCESS_NAME);
                var regularProcesses = _processProvider.FindProcessByName(ProcessProvider.LIDARR_PROCESS_NAME);

                _logger.Info("INSTANCE_DETECT Found {0} console processes and {1} regular processes", consoleProcesses.Count, regularProcesses.Count);
                Console.WriteLine("INSTANCE_DETECT Found {0} console processes and {1} regular processes", consoleProcesses.Count, regularProcesses.Count);

                var otherProcesses = consoleProcesses
                                     .Union(regularProcesses)
                                     .Select(c =>
                                     {
                                         _logger.Info("INSTANCE_DETECT Found Lidarr process ID {0} Name {1} StartPath {2}", c.Id, c.Name, c.StartPath ?? "unknown");
                                         Console.WriteLine("INSTANCE_DETECT Found Lidarr process ID {0} Name {1} StartPath {2}", c.Id, c.Name, c.StartPath ?? "unknown");
                                         return c.Id;
                                     })
                                     .Except(new[] { currentId })
                                     .ToList();

                if (otherProcesses.Any())
                {
                    _logger.Info("INSTANCE_DETECT Found {0} other Lidarr instances running", otherProcesses.Count);
                    Console.WriteLine("INSTANCE_DETECT Found {0} other Lidarr instances running", otherProcesses.Count);

                    foreach (var pid in otherProcesses)
                    {
                        _logger.Info("INSTANCE_DETECT Other instance process ID: {0}", pid);
                        Console.WriteLine("INSTANCE_DETECT Other instance process ID: {0}", pid);
                    }
                }
                else
                {
                    _logger.Info("INSTANCE_DETECT No other Lidarr instances detected");
                    Console.WriteLine("INSTANCE_DETECT No other Lidarr instances detected");
                }

                return otherProcesses;
            }
            catch (Exception ex)
            {
                _logger.Info("INSTANCE_DETECT Failed to check for multiple instances: {0}", ex.Message);
                Console.WriteLine("INSTANCE_DETECT Failed to check for multiple instances: {0}", ex.Message);
                return new List<int>();
            }
        }
    }
}
