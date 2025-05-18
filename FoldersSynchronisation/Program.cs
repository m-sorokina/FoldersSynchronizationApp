using Serilog;
using System.Diagnostics;

namespace FoldersSynchronization
{
    internal class Program
    {
        static void Main(string[] args)
        {

            if (args.Length < 4 || args.Length > 6)
            {
                Console.WriteLine("Usage: <source folder> <replica folder> <log file> <sync interval (seconds)> [log level] [dry run]");
                Environment.Exit(1);
            }

            string sourceFolder = args[0];
            string replicaFolder = args[1];
            string logFile = args[2];
            string syncIntervalArg = args[3];
            string? logLevelArg = args.Length > 4 ? args[4] : "inf";
            bool dryRun = args.Length == 6 && args[5].Equals("dryrun", StringComparison.OrdinalIgnoreCase);

            if (ValidateArguments(sourceFolder, replicaFolder, logFile, logLevelArg, syncIntervalArg,  out int syncInterval))
            {
                var session = new SynchronizeSession
                {
                    DryRun = false, //dryRun,
                    Logger = Log.Logger
                };
                while (true)
                {
                    Log.Information($"Synchronization is starting");
                    session.Synchronize(sourceFolder,replicaFolder);
                    Log.Information($"Synchronization completed\n");
                    Thread.Sleep(syncInterval * 1000);
                }

            }
            Log.Information("Application shutdown complete");
        }

        private static bool ValidateArguments(string sourceFolder, string replicaFolder, string logFile, string logLevelArg, string syncIntervalArg, out int syncInterval)
        {
            syncInterval = 0;
            try
            {
                string? logDir = Path.GetDirectoryName(logFile);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                using (var stream = File.Open(logFile, FileMode.Append, FileAccess.Write)) { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not create or write to log file '{logFile}'. {ex.Message}");
                Environment.Exit(1);
            }

            Serilog.Events.LogEventLevel logLevel = logLevelArg.ToLower() switch
            {
                "dbg" => Serilog.Events.LogEventLevel.Debug,
                "inf" => Serilog.Events.LogEventLevel.Information,
                "wrn" => Serilog.Events.LogEventLevel.Warning,
                "err" => Serilog.Events.LogEventLevel.Error,
                _ => Serilog.Events.LogEventLevel.Information
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3} - {Message:lj}{NewLine}")
                .WriteTo.File(logFile, rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3} - {Message:lj}{NewLine}")
                .CreateLogger();
            Log.Information("Application is starting");

            if (!Directory.Exists(sourceFolder))
            {
                Log.Error($"Source folder '{sourceFolder}' does not exist");
                return false;
            }


            if (!int.TryParse(syncIntervalArg, out syncInterval) || syncInterval < 0)
            {
                Log.Error("Synchronization interval must be a positive integer (in seconds) greater than 0");
                return false;
            }


            Log.Information("Argument validation completed successfully");
            return true;

        }

    }
}
