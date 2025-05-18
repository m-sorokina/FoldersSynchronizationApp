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
        enum Action { Skip, Copy, Update, Delete }

        private class FileAction
        {
            public string? FileName { get; set; } = null;
            public Action Action { get; set; } = Action.Skip;
        }
        public string SourceFolder { get; set; }
        public string ReplicaFolder { get; set; }
        public bool DryRun { get; set; }

        public ILogger? Logger { get; set; }

        public void Synchronize(string sourceFolderPath, string replicaFolderPath)
        {
            Logger?.Information($"Synchronizing source folder: '{sourceFolderPath}'");
            Logger?.Information($"Replica folder: '{replicaFolderPath}'");
            try
            {

                if (!Directory.Exists(replicaFolderPath))
                {

                    {
                        Directory.CreateDirectory(replicaFolderPath);
                        Log.Information($"Replica folder '{replicaFolderPath}' created successfully");
                    }
                }
                List<string> sourceFiles = [.. Directory.GetFiles(sourceFolderPath).Select(Path.GetFileName)];
                Logger?.Debug($"Files in source folder: {(sourceFiles.Count > 0 ? string.Join(", ", sourceFiles) : "no files in source folder")}");
                List<string> replicaFiles = [.. Directory.GetFiles(replicaFolderPath).Select(Path.GetFileName)];
                Logger?.Debug($"Files in replica folder: {(replicaFiles.Count > 0 ? string.Join(", ", replicaFiles) : "no files in replica folder")}");
                List<FileAction> filesToAdd = [..sourceFiles.Except(replicaFiles)
                    .Select(f => new FileAction{ FileName = f, Action = Action.Copy })];
                List<FileAction> filesToRemove = [..replicaFiles.Except(sourceFiles)
                    .Select(f => new FileAction{ FileName = f, Action = Action.Delete})];
                List<FileAction> commonFiles = [..sourceFiles.Intersect(replicaFiles)
                    .Select(f => new FileAction { FileName = f})];
                filesToAdd.AddRange(CompareFiles(commonFiles, sourceFolderPath, replicaFolderPath));
                Logger?.Debug($"Files to synchronize: {(filesToAdd.Count > 0 ? string.Join(", ", filesToAdd.Select(f => f.FileName)) : "no files to synchronize")}");
                foreach (var file in filesToAdd)
                {
                    try
                    {
                        string sourceFilePath = Path.Combine(sourceFolderPath, file.FileName);
                        string replicaFilePath = Path.Combine(replicaFolderPath, file.FileName);
                        if (DryRun)
                        {
                            if (file.Action == Action.Copy)
                            {
                                Logger?.Information($"[DRY RUN] Would copy: '{file.FileName}'");
                            }
                            else
                            {
                                Logger?.Information($"[DRY RUN] Would update: '{file.FileName}'");
                            }
                        }
                        else
                        {

                            File.Copy(sourceFilePath, replicaFilePath, true);
                            if (file.Action == Action.Copy)
                            {
                                Logger?.Information($"Copied: '{file.FileName}'");
                            }
                            else
                            {
                                Logger?.Information($"Updated: '{file.FileName}'");
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Logger?.Error($"Failed to copy/update file '{file.FileName}': {ex.Message}");
                        Logger?.Warning($"File '{file.FileName}' will be skipped");
                    }
                }
                foreach (var file in filesToRemove)
                {
                    try
                    {
                        string replicaFilePath = Path.Combine(replicaFolderPath, file.FileName);
                        if (DryRun)
                        {
                            Logger?.Information($"[DRY RUN] Would delete: '{file.FileName}'");
                        }
                        else
                        {

                            File.Delete(replicaFilePath);
                            Logger?.Information($"Deleted: '{file.FileName}'");
                        }


                    }
                    catch (Exception ex)
                    {
                        Logger?.Error($"Failed to delete file '{file.FileName}': {ex.Message}");
                        Logger?.Warning($"File '{file.FileName}' will be skipped");

                    }
                }
                List<string> sourceFolders = [.. Directory.GetDirectories(sourceFolderPath).Select(Path.GetFileName)];
                Logger?.Debug($"Subfolders in source folder: {(sourceFolders.Count > 0 ? string.Join(", ", sourceFolders) : "no subfolders in source folder")}");
                List<string> replicaFolders = [.. Directory.GetDirectories(replicaFolderPath).Select(Path.GetFileName)];
                Logger?.Debug($"Subfolders in replica folder: {(replicaFolders.Count > 0 ? string.Join(", ", replicaFolders) : "no subfolders in replica folder")}");
                foreach (var folder in sourceFolders)
                {
                    string sourceSubfolderPath = Path.Combine(sourceFolderPath, folder);
                    string replicaSubfolderPath = Path.Combine(replicaFolderPath, folder);
                    if (DryRun)
                    {
                        Logger?.Information($"[DRY RUN] Would synchronize subfolder: '{replicaSubfolderPath}'");
                        Synchronize(sourceSubfolderPath, replicaSubfolderPath);
                    }
                    else
                    {
                        Synchronize(sourceSubfolderPath, replicaSubfolderPath);
                    }
                }
                var foldersToRemove = replicaFolders.Except(sourceFolders).ToList();
                foreach (var folder in foldersToRemove)
                {

                    string replicaSubfolderPath = Path.Combine(replicaFolderPath, folder);
                    try
                    {
                        if (DryRun)
                        {
                            Logger?.Information($"[DRY RUN] Would delete folder: '{replicaSubfolderPath}'");
                        }
                        else
                        {
                            Directory.Delete(replicaSubfolderPath, true);
                            Logger?.Information($"Deleted folder: '{replicaSubfolderPath}'");
                        }

                    }
                    catch (Exception ex)
                    {
                        Logger?.Error($"Failed to delete folder '{replicaSubfolderPath}': {ex.Message}");
                        Logger?.Warning($"Folder '{replicaSubfolderPath}' will be skipped");
                    }
                }

            }
            catch (Exception ex)
            {
                Logger?.Error($"Failed to perform operation: {ex.Message}");
            }


        }

        private List<FileAction> CompareFiles(List<FileAction> commonFiles, string sourceFolder, string replicaFolder)
        {
            List<FileAction> filesToUpdate = new();
            foreach (var file in commonFiles)
            {
                try
                {

                    string sourceFilePath = Path.Combine(sourceFolder, file.FileName);
                    string replicaFilePath = Path.Combine(replicaFolder, file.FileName);

                    var sourceFileInfo = new FileInfo(sourceFilePath);
                    var replicaFileInfo = new FileInfo(replicaFilePath);
                    if (sourceFileInfo.Length != replicaFileInfo.Length)
                    {
                        file.Action = Action.Update;
                        filesToUpdate.Add(file);
                        Logger?.Debug($"File '{file.FileName}' differs by size: source={sourceFileInfo.Length} bytes, replica={replicaFileInfo.Length} bytes");
                    }
                    else
                    {
                        if (!AreFilesEqualByHashes(sourceFilePath, replicaFilePath))
                        {
                            file.Action = Action.Update;
                            filesToUpdate.Add(file);
                            Logger?.Debug($"File '{file.FileName}' differs by hash codes");
                        }
                        Logger?.Debug($"Skipped: '{file.FileName}' (no changes)");
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Error($"Failed to compare file: {ex.Message}");
                    Logger?.Warning($"File '{file.FileName}' will be skipped");
                }


            }
            return filesToUpdate;
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
