## Folder Synchronization Console App (C#)

A simple C# console application that synchronizes data from a source folder to a replica folder:
- One-way synchronization from source to replica  
- Allows setting the synchronization interval  
- Optional log level setting  
- Optional safe ‘dry run’ mode (for files; folders are still created)  
- Supports nested folders and basic error handling  
- Compares file size and hash codes

Usage: <source_folder> <replica_folder> <log_file> <sync interval (seconds)> [log level] [dry run]
____________________________________________________________________________________________________
### Notes:  
1. The project is synchronous
1. Command-line execution: manual argument parsing was used to keep the solution simple  
    > If more complex configuration is required, a more flexible configuration system could be introduced 
1. Argument validation: basic validation ensures that source, replica and log paths are not equal or subfolders of one another  
1. Synchronization interval  
   - The application performs an initial synchronization, waits for the specified interval, then repeats the process  
   - Value of 0 is not allowed for the interval, there is no upper limit  
    > In more advanced scenarios, synchronization could be scheduled instead of timed  
    > However, that would require handling sync duration more carefully - for example, if the source folder grows significantly, a single sync cycle may take longer than the interval itself  
1. Synchronization process: the core behavior and basic error handling were implemented as follows:  
     - all files in the source folder are scanned and grouped by name: to copy (missing in replica), to remove (missing in source), to check additionally (exist in both)  
     - files that exist in both folders are compared by size and hash code  
     - appropriate actions are performed to modify the replica folder  
     - subfolders are processed: checked for existence in source, grouped into to check and to remove, excess folders are deleted  
     - the above file and folder sync logic is repeated recursively for all subfolders 
     - in the case of I/O or access errors, the affected file or folder is skipped and the process continues  
     - skipped items are logged at the DEBUG level
1. Additional considerations  
     - hidden/system files and folders are currently not filtered - they will be copied  
     - to skip them, an additional filter should be implemented, the same as a parameter to exclude certain file types    
     - symbolic links are not handled  

### Cases not covered in this implementation:
   - if many files are deleted in the source during a sync cycle, should all replica files be removed automatically, or should a confirmation be required?
   - if the source folder is renamed or deleted, should the replica folder be deleted?
   - behavior when the sync process is interrupted (e.g., closed mid-run)
   - handling files being updated during sync (locked files are skipped and processed in the next sync cycle)  
   - logging or measuring sync duration/performance
   - while attributes and metadata are copied alongside files, changes of metadata/attributes alone do not trigger file syncing, since only file size and hash are compared
