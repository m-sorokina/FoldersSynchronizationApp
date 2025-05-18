using Serilog;
using System.Diagnostics;

namespace FoldersSynchronization
{
    internal class Program
    {
        static string _sourceFolder = string.Empty;
        static string _replicaFolder = string.Empty;
        static string _logFile = string.Empty;
        static int _syncInterval = 0;
        static bool _dryRun = false;
        static void Main(string[] args)
        {
            if (ValidateArguments(args))
            {
                Log.Information("Synchronization is starting...");
                while (true)
                {
                    SynchronizeSession();
                    Log.Information($"Synchronization completed");
                    Thread.Sleep(_syncInterval * 1000);
                }

            }
            Log.Information("Application shutdown complete");
        }

        private static bool ValidateArguments(string[] args)
        {
            if (args.Length < 4 || args.Length > 5)
            {
                Console.WriteLine("Usage: <source folder> <replica folder> <log file> <sync interval (seconds)> [dry run]");
                Environment.Exit(1);
            }

            _sourceFolder = args[0];
            _replicaFolder = args[1];
            _logFile = args[2];
            string intervalArg = args[3];
            _dryRun = args.Length == 5 && args[4].Equals("dryrun", StringComparison.OrdinalIgnoreCase);

            try
            {
                string? logDir = Path.GetDirectoryName(_logFile);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                using (var stream = File.Open(_logFile, FileMode.Append, FileAccess.Write)) { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not create or write to log file '{_logFile}'. {ex.Message}");
                Environment.Exit(1);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3} - {Message:lj}{NewLine}")
                .WriteTo.File(_logFile, rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3} - {Message:lj}{NewLine}")
                .CreateLogger();
            Log.Information("Application is starting");

            if (!Directory.Exists(_sourceFolder))
            {
                Log.Error($"Source folder '{_sourceFolder}' does not exist");
                return false;
            }

            try
            {
                if (!Directory.Exists(_replicaFolder))
                {
                    Directory.CreateDirectory(_replicaFolder);
                    Log.Information($"Replica folder '{_replicaFolder}' created successfully");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Could not create replica folder '{_replicaFolder}'. {ex.Message}");
                return false;
            }


            if (!int.TryParse(intervalArg, out _syncInterval) || _syncInterval < 0)
            {
                Log.Error("Synchronization interval must be a positive integer (in seconds) greater than 0");
                return false;
            }


            Log.Information("Argument validation completed successfully");
            return true;

        }

        private static void SynchronizeSession()
        { }
    }
}
