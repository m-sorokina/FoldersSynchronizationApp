using Serilog;

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

            if (ValidateArguments(sourceFolder, replicaFolder, logFile, logLevelArg, syncIntervalArg, dryRun, out int syncInterval))
            {
                var session = new SynchronizeSession
                {
                    DryRun = dryRun,
                    Logger = Log.Logger
                };

                while (true)
                {
                    Log.Information($"Synchronization is starting...");
                    session.Synchronize(sourceFolder, replicaFolder);
                    Log.Information($"Synchronization completed\n");
                    Thread.Sleep(syncInterval * 1000);
                }

            }
            Log.Information("Application shutdown complete\n");
        }

        private static bool ValidateArguments(string sourceFolder, string replicaFolder, string logFile, string logLevelArg, string syncIntervalArg, bool dryRun, out int syncInterval)
        {
            syncInterval = 0;

            string sourceFolderFullPath = Path.GetFullPath(sourceFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string replicaFolderFullPath = Path.GetFullPath(replicaFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string logFileFullPath = Path.GetFullPath(logFile).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            try
            {

                if (logFileFullPath.Equals(sourceFolderFullPath, StringComparison.OrdinalIgnoreCase) ||
                    logFileFullPath.StartsWith(sourceFolderFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    logFileFullPath.Equals(replicaFolderFullPath, StringComparison.OrdinalIgnoreCase) ||
                    logFileFullPath.StartsWith(replicaFolderFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Log file must not be located inside the source or the replica folder\n" +
                                                $"Loaded configuration: \n" +
                                                $"- Source folder  : {sourceFolder}\n" +
                                                $"- Replica folder : {replicaFolder}\n" +
                                                $"- Log file path  : {logFile}\n");
                }
                string? logDir = Path.GetDirectoryName(logFile);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                using (var stream = File.Open(logFile, FileMode.Append, FileAccess.Write)) { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: unable to create or write to the log file '{logFile}'\n{ex.Message}");
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
            Log.Information("""
                Loaded configuration: 
                  - Source folder  : {sourceFolder}
                  - Replica folder : {replicaFolder}
                  - Sync interval  : {syncInterval}
                  - Log file path  : {logFile}
                  - Log level      : {logLevelArg}
                  - Dry run mode   : {dryRun}                
                """, sourceFolder, replicaFolder, syncInterval, logFile, logLevelArg.ToUpper(), dryRun);


            if (!Directory.Exists(sourceFolder))
            {
                Log.Error($"Source folder '{sourceFolder}' does not exist");
                return false;
            }

            if (sourceFolderFullPath.Equals(replicaFolderFullPath, StringComparison.OrdinalIgnoreCase))
            {
                Log.Error("Source and replica folders must be different");
                return false;
            }
            if (replicaFolderFullPath.StartsWith(sourceFolderFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                Log.Error("Replica folder must not be a subfolder of the source folder");
                return false;
            }
            if (sourceFolderFullPath.StartsWith(replicaFolderFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                Log.Error("Source folder must not be a subfolder of the replica folder");
                return false;
            }


            if (!int.TryParse(syncIntervalArg, out syncInterval) || syncInterval < 0)
            {
                Log.Error("Synchronization interval must be a positive integer (in seconds) greater than 0");
                return false;
            }


            Log.Information("Configuration validation completed successfully");
            return true;

        }

    }
}
