<h1 align="center">UnityFileSplitter by David Westberg</h1>

## Overview
A simple&easy to use alternative to git LFS for unity that works with any github repo. Splits large files into multiple smaller files, automatically adds the large files to your .gitignore and merges the splitted files back into their orginal large file.

<img src="https://i.postimg.cc/FKWKv9Tb/file-Splitter-Image.png" width="100%" height="100%"/>

## Key Features
<ul>
<li>Split large files into multiple smaller files</li>
<li>Merge splitted files back into their orginal large file</li>
<li>Adds splitted large files to your .gitignore</li>
<li>Exclude certain folders and file types from being splitted</li>
<li>Validates splitted files before merging so no data is missing</li>
</ul>

## Instructions
**Requirements** (Should work in other versions)
<ul>
<li>Unity 2023.2.20f1</li>
</ul>

**How To Use**
<ol>
    <li>Download and copy the Resources and Scripts folders into an empty folder inside your Assets folder</li>
    <li>Open the <code>Scripts/SplitConfig.cs</code> file and configure it, its recommended to add the folder name your .gitignore file is inside to the requiredGitIgnoreFolderName hashset</li>
    <li>Make sure Unity is closed before you commit anything to github</li>
</ol>

## Technical Details
**Splitting Files**

A file is splitted by reading all of its bytes into an array and writing every `SplitConfig.splitFilesLargerThanMB * 1000000` byte to a seperate file in a temporary folder. The path to the files is then saved inside a ScriptableObject.
Adds the orginal file path to the .gitignore file, a copy of the .gitignore file is created before modifying it. See `DoSplitFiles()` in `Scripts/FileSplitter.cs` for more details

**Merging Files**

Loops through all saved file paths in the ScriptableObject and verifies all splitted files still exists. Reads the splitted files bytes back into a single array, verifies that no bytes are missing and overwrites the orginal file.
Restores the .gitignore file from the copy created before modifying it and deletes all files in the temporary folder. See `DoMergeFiles()` in `Scripts/FileSplitter.cs` for more details

## License
This project is licensed under MIT - See the `LICENSE` file for more details.
