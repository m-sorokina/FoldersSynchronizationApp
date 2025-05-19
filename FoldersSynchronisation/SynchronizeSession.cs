using Serilog;
using System.Data;
using System.Security.Cryptography;

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
            Logger?.Information($"Synchronizing folder: '{sourceFolderPath}'");
            Logger?.Information($"To folder: '{replicaFolderPath}'");
            try
            {

                if (!Directory.Exists(replicaFolderPath))
                {

                    {
                        Directory.CreateDirectory(replicaFolderPath);
                        Directory.SetCreationTime(replicaFolderPath, Directory.GetCreationTime(sourceFolderPath));
                        Directory.SetLastAccessTime(replicaFolderPath, Directory.GetLastAccessTime(sourceFolderPath));
                        Directory.SetLastWriteTime(replicaFolderPath, Directory.GetLastWriteTime(sourceFolderPath));
                        Logger?.Information($"Replica folder '{replicaFolderPath}' created successfully");
                    }
                }
                Logger?.Information($"Scanning source folder '{sourceFolderPath}'");
                List<string> sourceFiles = [.. Directory.GetFiles(sourceFolderPath).Select(Path.GetFileName)];
                Logger?.Debug($"Files in source folder: {(sourceFiles.Count > 0 ? string.Join(", ", sourceFiles) : "no files in source folder")}");
                Logger?.Information($"Scanning replica folder '{replicaFolderPath}'");
                List<string> replicaFiles = [.. Directory.GetFiles(replicaFolderPath).Select(Path.GetFileName)];
                Logger?.Debug($"Files in replica folder: {(replicaFiles.Count > 0 ? string.Join(", ", replicaFiles) : "no files in replica folder")}");

                List<FileAction> filesToAdd = [..sourceFiles.Except(replicaFiles)
                    .Select(f => new FileAction{ FileName = f, Action = Action.Copy })];
                List<FileAction> filesToRemove = [..replicaFiles.Except(sourceFiles)
                    .Select(f => new FileAction{ FileName = f, Action = Action.Delete})];
                List<FileAction> commonFiles = [..sourceFiles.Intersect(replicaFiles)
                    .Select(f => new FileAction { FileName = f})];
                filesToAdd.AddRange(CompareFiles(commonFiles, sourceFolderPath, replicaFolderPath));
                var filesToUpdateCount = filesToAdd.Count(f => f.Action == Action.Update);
                var filesToCopyCount = filesToAdd.Count(f => f.Action == Action.Copy);
                var filesToDeleteCount = filesToRemove.Count;
                Logger?.Debug($"Files to synchronize: {((filesToAdd.Count + filesToRemove.Count) > 0 ? string.Join(", ", filesToAdd.Concat(filesToRemove).Select(f => f.FileName)) : "no files to synchronize")}");
                Logger?.Information($"Files to update: {(filesToUpdateCount > 0 ? filesToUpdateCount : 0)}, to copy: {(filesToCopyCount > 0 ? filesToCopyCount : 0)}, to delete: {(filesToDeleteCount > 0 ? filesToDeleteCount : 0)}");
                CopyUpdateFiles(filesToAdd, sourceFolderPath, replicaFolderPath);
                DeleteFiles(filesToRemove, sourceFolderPath, replicaFolderPath);

                List<string> sourceFolders = [.. Directory.GetDirectories(sourceFolderPath).Select(Path.GetFileName)];
                Logger?.Debug($"Subfolders in source folder: {(sourceFolders.Count > 0 ? string.Join(", ", sourceFolders) : "no subfolders in source folder")}");
                List<string> replicaFolders = [.. Directory.GetDirectories(replicaFolderPath).Select(Path.GetFileName)];
                Logger?.Debug($"Subfolders in replica folder: {(replicaFolders.Count > 0 ? string.Join(", ", replicaFolders) : "no subfolders in replica folder")}");
                var foldersToRemove = replicaFolders.Except(sourceFolders).ToList();
                Logger?.Debug($"Subfolders to synchronize: {((sourceFolders.Count + foldersToRemove.Count) > 0 ? string.Join(", ", sourceFolders.Concat(foldersToRemove)) : "no subfolders to synchronize")}");
                Logger?.Information($"Subfolders to update: {(sourceFolders.Count > 0 ? sourceFolders.Count : 0)}, to delete: {(foldersToRemove.Count > 0 ? foldersToRemove.Count : 0)}");
                DeleteSubfolders(foldersToRemove, replicaFolderPath);
                UpdateSubfolders(sourceFolders, sourceFolderPath, replicaFolderPath);
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
                        else
                        {
                            Logger?.Debug($"Skipped: '{file.FileName}' (no changes)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Error($"Failed to compare file: {ex.Message}");
                    Logger?.Warning($"File '{file.FileName}' will be skipped (error)");
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

        private void CopyUpdateFiles(List<FileAction> filesToAdd, string sourceFolderPath, string replicaFolderPath)
        {
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
                        File.SetAttributes(replicaFilePath, File.GetAttributes(sourceFilePath));
                        File.SetCreationTime(replicaFilePath, File.GetCreationTime(sourceFilePath));
                        File.SetLastAccessTime(replicaFilePath, File.GetLastAccessTime(sourceFilePath));
                        File.SetLastWriteTime(replicaFilePath, File.GetLastWriteTime(sourceFilePath));

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
                    Logger?.Warning($"File '{file.FileName}' will be skipped (error)");
                }
            }
        }

        private void DeleteFiles(List<FileAction> filesToRemove, string sourceFolderPath, string replicaFolderPath)
        {
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
                    Logger?.Warning($"File '{file.FileName}' will be skipped (error)");

                }
            }
        }

        private void DeleteSubfolders(List<string> foldersToRemove, string replicaFolderPath)
        {
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
                    Logger?.Warning($"Folder '{replicaSubfolderPath}' will be skipped (error)");
                }
            }
        }

        private void UpdateSubfolders(List<string> sourceFolders, string sourceFolderPath, string replicaFolderPath)
        {
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
        }

    }
}
