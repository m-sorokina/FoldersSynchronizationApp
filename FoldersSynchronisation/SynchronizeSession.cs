using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FoldersSynchronization
{
    public class SynchronizeSession
    {
        public string SourceFolder { get; set; }
        public string ReplicaFolder { get; set; }
        public bool DryRun { get; set; }

        public ILogger? Logger { get; set; }

        public void Start()
        {
            try
            {
                List<string> sourceFiles = [..Directory.GetFiles(SourceFolder).Select(Path.GetFileName)];
                Logger?.Debug($"Source files: {string.Join(", ", sourceFiles)}");
                List<string> replicaFiles = [..Directory.GetFiles(ReplicaFolder).Select(Path.GetFileName)];
                Logger?.Debug($"Replica files: {string.Join(", ", replicaFiles)}");
                var filesToAdd = sourceFiles.Except(replicaFiles).ToList();
                var filesToRemove = replicaFiles.Except(sourceFiles).ToList();
                var commonFiles = sourceFiles.Intersect(replicaFiles).ToList();
                filesToAdd.AddRange(CompareFiles(commonFiles));
                Logger?.Debug($"Files to sync: {(filesToAdd.Count > 0 ? string.Join(", ", filesToAdd):"no files to sync")}");
                foreach (var fileName in filesToAdd)
                {
                    string sourceFilePath = Path.Combine(SourceFolder, fileName);
                    string replicaFilePath = Path.Combine(ReplicaFolder, fileName);
                    if (DryRun)
                    {
                        Logger?.Information($"[DRY RUN] Would copy '{sourceFilePath}' to '{replicaFilePath}'");
                    }
                    else
                    {
                        File.Copy(sourceFilePath, replicaFilePath, true);
                        Logger?.Information($"Copied '{sourceFilePath}' to '{replicaFilePath}'");
                    }
                }
                foreach (var fileName in filesToRemove)
                {
                    string replicaFilePath = Path.Combine(ReplicaFolder, fileName);
                    if (DryRun)
                    {
                        Logger?.Information($"[DRY RUN] Would delete '{replicaFilePath}'");
                    }
                    else
                    {
                        File.Delete(replicaFilePath);
                        Logger?.Information($"Deleted '{replicaFilePath}'");
                    }
                }

            }
            catch (Exception ex)
            {
                Logger?.Error($"Failed to retrieve file names from folder: {ex.Message}");
            }


            

        }

        private List<string> CompareFiles(List<string> commonFiles)
        { 
            List<string> filesToSync = new ();
            foreach (var fileName in commonFiles)
            {
                string sourceFilePath = Path.Combine(SourceFolder, fileName);
                string replicaFilePath = Path.Combine(ReplicaFolder, fileName);

                var sourceFileInfo = new FileInfo(sourceFilePath);
                var replicaFileInfo = new FileInfo(replicaFilePath);
                if (sourceFileInfo.Length != replicaFileInfo.Length)
                { 
                filesToSync.Add(fileName);
                    Logger?.Debug($"File '{fileName}' differs by size: Source={sourceFileInfo.Length} bytes, Replica={replicaFileInfo.Length} bytes");
                }
                else 
                {
                    if (!AreFilesEqualByHashes(sourceFilePath, replicaFilePath))
                    {
                        filesToSync.Add(fileName);
                        Logger?.Debug($"File '{fileName}' differs by hash codes");
                    }
                }

            }
            return filesToSync;
        }

        static private bool AreFilesEqualByHashes(string sourceFilePath, string replicaFilePath)
        {
            using var sha256 = SHA256.Create();
            using var streamSourceFile = File.OpenRead(sourceFilePath);
            using var streamReplicaFile = File.OpenRead(replicaFilePath);

            var hash1 = sha256.ComputeHash(streamSourceFile);
            var hash2 = sha256.ComputeHash(streamReplicaFile);

            return hash1.SequenceEqual(hash2);

        }

    }
}
