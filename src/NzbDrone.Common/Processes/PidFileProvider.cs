using System;
using System.IO;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Common.Processes
{
    public interface IProvidePidFile
    {
        void Write();
    }

    public class PidFileProvider : IProvidePidFile
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly Logger _logger;

        public PidFileProvider(IAppFolderInfo appFolderInfo, Logger logger)
        {
            _appFolderInfo = appFolderInfo;
            _logger = logger;
        }

        public void Write()
        {
            _logger.Info("PIDFILE Starting PID file write process");
            Console.WriteLine("PIDFILE Starting PID file write process");

            if (OsInfo.IsWindows)
            {
                _logger.Info("PIDFILE Skipping PID file creation on Windows platform");
                Console.WriteLine("PIDFILE Skipping PID file creation on Windows platform");
                return;
            }

            var currentPid = ProcessProvider.GetCurrentProcessId();
            var filename = Path.Combine(_appFolderInfo.AppDataFolder, "lidarr.pid");

            _logger.Info("PIDFILE Creating PID file {0} with process ID {1}", filename, currentPid);
            Console.WriteLine("PIDFILE Creating PID file {0} with process ID {1}", filename, currentPid);

            // Check if PID file already exists
            if (File.Exists(filename))
            {
                try
                {
                    var existingPidText = File.ReadAllText(filename);
                    if (int.TryParse(existingPidText, out var existingPid))
                    {
                        _logger.Info("PIDFILE Found existing PID file with process ID {0}", existingPid);
                        Console.WriteLine("PIDFILE Found existing PID file with process ID {0}", existingPid);

                        // Check if the process with that PID is still running
                        try
                        {
                            var existingProcess = System.Diagnostics.Process.GetProcessById(existingPid);
                            _logger.Info("PIDFILE WARNING: Process {0} is still running with name {1}", existingPid, existingProcess.ProcessName);
                            Console.WriteLine("PIDFILE WARNING: Process {0} is still running with name {1}", existingPid, existingProcess.ProcessName);
                        }
                        catch (ArgumentException)
                        {
                            _logger.Info("PIDFILE Process {0} from existing PID file is no longer running, safe to overwrite", existingPid);
                            Console.WriteLine("PIDFILE Process {0} from existing PID file is no longer running, safe to overwrite", existingPid);
                        }
                    }
                    else
                    {
                        _logger.Info("PIDFILE Existing PID file contains invalid data: {0}", existingPidText);
                        Console.WriteLine("PIDFILE Existing PID file contains invalid data: {0}", existingPidText);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Info("PIDFILE Failed to read existing PID file: {0}", ex.Message);
                    Console.WriteLine("PIDFILE Failed to read existing PID file: {0}", ex.Message);
                }
            }
            else
            {
                _logger.Info("PIDFILE No existing PID file found at {0}", filename);
                Console.WriteLine("PIDFILE No existing PID file found at {0}", filename);
            }

            try
            {
                File.WriteAllText(filename, currentPid.ToString());
                _logger.Info("PIDFILE Successfully wrote PID file {0} with process ID {1}", filename, currentPid);
                Console.WriteLine("PIDFILE Successfully wrote PID file {0} with process ID {1}", filename, currentPid);
            }
            catch (Exception ex)
            {
                _logger.Info("PIDFILE Unable to write PID file {0}: {1}", filename, ex.Message);
                Console.WriteLine("PIDFILE Unable to write PID file {0}: {1}", filename, ex.Message);
                throw new LidarrStartupException(ex, "Unable to write PID file {0}", filename);
            }
        }
    }
}
