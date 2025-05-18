**General Task**  
Please implement a program that synchronizes two folders: source and replica.
The program should maintain a full, identical copy of source folder at replica
folder.
Solve the test task by writing a program in C#.

Synchronization must be one-way: after the synchronization content of the replica
folder should be modified to exactly match content of the source folder;
Synchronization should be performed periodically;
File creation/copying/removal operations should be logged to a file and to the
console output;
Folder paths, synchronization interval and log file path should be provided using
the command line arguments;
It is undesirable to use third-party libraries that implement folder synchronization;

It is allowed (and recommended) to use external libraries implementing other well-
known algorithms. For example, there is no point in implementing yet

another function that calculates MD5 if you need it for the task – it is perfectly
acceptable to use a third-party (or built-in) library;
The solution should be presented in the form of a link to the public GitHub repository.

__________________________________________________________________________________________
**Scenario:**
1. Enter command line arguments in the console:	source folder, replica folder, log file, synchronization interval, dry run (optional)
   a. Source folder - path to the source folder (e.g. C:\Users\user\Documents\source)
   b. Replica folder - path to the replica folder (e.g. C:\Users\user\Documents\replica)
   c. Log file - path to the log file (e.g. C:\Users\user\Documents\log.txt)
   d. Synchronization interval - time in seconds (e.g. 5 seconds)
   e. Dry run - optional parameter, if present, the program will only log the actions without performing them	
2. Check parameters:
   a. Check the whole number of parameters - if not, exit the program with an error message
   b. Check if the source folder exists - if not, exit the program with an error message
   c. Check if the replica folder exists - if not, create it - if write error, exit the program with an error message
   d. Check if the log file path is valid - if not create it - if write error, exit the program with an error message (maybe it should be created first and other errors be recoded to it)
   e. Check if the synchronization interval is valid (positive integer)
3. Perform synchronization of the source folder with the replica folder: 
   a. Compare the folders and files in the source and replica folders (hash codes, timestamp?, file size)
   b. Create folders in the replica folder that do not exist - if I/O/A error, record error in logs and go to the next folder
   c. Copy new files from the source to the replica folder - if I/O/A error, record error in logs and go to the next file
   d. Remove files from the replica folder that are not in the source folder - if I/O/A error, record error in logs and go to the next file
   e. Update files in the replica folder that have been modified in the source folder - if I/O/A error, record error in logs and go to the next file
4. Log the synchronization process to the log file and console
5. Synchronize the folders periodically (loop)

___________________________________________________________________________________________
**Application Data:**
1. Source folder
2. Replica folder
3. Log file/on console
4. Synchronization interval - select type of time (ms, s, minutes)
5. File with hash codes?

____________________________________________________________________________________________
**Steps to implement:**
1. Parse command line arguments and check parameters for errors - done
1. Log output to the console and log file - done
1. Create a loop for synchronization - done
1. Create a method for synchronization a folder with only files: 
	a. compare - done?
	b. create folder - done
	c. copy file - done
	d. remove folder - done
	e. remove file - done
	f. hash code processing
1. Create a method for synchronization with nested folders (folders and subfolders with files) - done
1. Create a test project with unit tests for the methods
1. Add additional arguments validation: 1,2,3,4 from questions validation section
1. Add configuration file for the application (e.g. appsettings.json) to store default values for the parameters
1. Add additional argument for log level changing - done
____________________________________________________________________________________________
**Questions:**  
General  
1. How the program should be ended? (Ctrl+C, close the console window, etc.)  
 
Validation arguments  
1. If replica folder is defined as a source folder, should the program stop?
1. If replica folder is defined as subfolder of the source folder, should the program stop?
1. If the source folder is defined as a subfolder of the replica folder, should the program stop?
1. The same question about log file - can it be a part of replica/source folder?
1. The synchronization interval should be checked not less than some time and not more than some time? (e.g. 1 second and 1 hour) - describe in notes  

Synchronization  
1. If the source folder in one of the syncs was cleared, should the program remove all files from the replica folder or ask a confirmation?
1. If the source folder was deleted, should the program remove all the replica folder including files or ask a confirmation?
1. What about hidden/system files/folders? Should they be copied or ignored? By default all the files are synchronized, additional filter is needed if such files shouldn't be copied
1. If synchronization interval is set to 0, should the program synchronize only once or infinitely? - solved in the code, must be greater than 0
1. If synchronization interval is less than the synchronization time? and synchronization time grows infinitely? - describe in notes possible solution
1. The case if file is editing during the synchronization process? Should the program wait for the file to be closed or skip it?
1. If the program is interrupted during the synchronization process, should it continue from the last point or start from the beginning?



