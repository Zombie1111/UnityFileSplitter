//Made by David Westberg
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace zombFiles
{
    class MyAllPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            //Setup auto split and merge
            var fSplitter = FileSplitter.TryGetSplitSaveFile();
            if (fSplitter == null)
            {
                Debug.LogError("Auto split and merge failed, no FillerSplitter found. Was TryGetSplitSaveFile() called before importing?");
                return;
            }

            fSplitter.SetupEditor();
        }
    }

    public class FileSplitter : ScriptableObject
    {
        private bool hasSetupEditor = false;

        public void SetupEditor()
        {
#pragma warning disable CS0162 // Unreachable code detected
            if (hasSetupEditor == true || SplitConfig.autoMergeAndSplit == false) return;
            hasSetupEditor = true;

            EditorApplication.quitting -= OnEditorClose;
            EditorApplication.quitting += OnEditorClose;
            OnEditorStart();
#pragma warning restore CS0162 // Unreachable code detected
        }

        public static void OnEditorStart()
        {
            TryGetSplitSaveFile().DoMergeFiles(false);
        }

        public static void OnEditorClose()
        {
            SplitFiles();
        }

        [SerializeField] private List<SplitFile> splittedFiles = new();
        [SerializeField] private string gitIgnorePath = null;

        [System.Serializable]
        public class SplitFile
        {
            public List<string> splitFileNames = new();
            public string sourceRelativePath = string.Empty;
            public long sourceByteSize = 0;
        }

        [MenuItem("Tools/File Splitting/Merge Files")]
        private static void MergeFiles()
        {
            TryGetSplitSaveFile().DoMergeFiles(true);//Also change in OnEditorStart()
        }

        [MenuItem("Tools/File Splitting/MX_JustToAddSpacing")]
        [MenuItem("Tools/File Splitting/SA_EvenMoreSpacing")]
        public static void MenuSpace()
        {
            Debug.Log("Just for spacing I said :D");
        }

        private string RestoreGitIgnore()
        {
            //Restore .gitIgnore
            string ignoreFullPath = TryGetGitIgnoreFullPath(out string splitFolderPath);
            if (ignoreFullPath == null) return null;

            string ogIgnoreFullPath = GetFullOgGitIgnorePath(ignoreFullPath);
            if (File.Exists(ogIgnoreFullPath) == true)
            {
                File.WriteAllBytes(ignoreFullPath, File.ReadAllBytes(ogIgnoreFullPath));
                File.Delete(ogIgnoreFullPath);
            }

            return splitFolderPath;
        }

        private void DoMergeFiles(bool logIfNoFiles = true)
        {
            //Check if we have files to merge
            if (splittedFiles.Count == 0)
            {
                if (logIfNoFiles == true) Debug.Log("Got no files to merge");
                return;
            }

            //Restore .gitIgnore
            string splitFolderPath = RestoreGitIgnore();
            if (splitFolderPath == null) return;

            //Merge the files
            foreach (SplitFile file in splittedFiles)
            {
                DoMergeFile(file);
            }

            Debug.Log("Merged " + splittedFiles.Count + " files");
            DeleteSplittedFiles(splitFolderPath);

            void DoMergeFile(SplitFile file)
            {
                //Verify all split files exist and get their data
                byte[] newBytes = new byte[file.sourceByteSize];
                long newBytesI = 0;

                foreach (string splitPathRel in file.splitFileNames)
                {
                    string splitPath = Path.Combine(splitFolderPath, splitPathRel);
                    if (File.Exists(splitPath) == false)
                    {
                        Debug.LogError("Missing split file at " + splitPath + " for source " + file.sourceRelativePath);
                        return;
                    }

                    byte[] splitBytes = File.ReadAllBytes(splitPath);
                    if (file.sourceByteSize < newBytesI + splitBytes.Length)
                    {
                        Debug.LogError("Split files" + file.splitFileNames[0] + " does not contain the correct amount of bytes for source " + file.sourceRelativePath + "   Expected " + file.sourceByteSize + " got " + (newBytesI + splitBytes.Length));
                        return;
                    }

                    foreach (byte splitByte in splitBytes)
                    {
                        newBytes[newBytesI] = splitByte;
                        newBytesI++;
                    }
                }

                if (newBytesI != file.sourceByteSize)
                {
                    Debug.LogError("Split files" + file.splitFileNames[0] + " does not contain the correct amount of bytes for source " + file.sourceRelativePath + "   Expected " + file.sourceByteSize + " got " + newBytesI);
                    return;
                }

                //Write to source
                string sourceDic = new FileInfo(file.sourceRelativePath).Directory.FullName;
                Directory.CreateDirectory(@sourceDic);
                File.WriteAllBytes(file.sourceRelativePath, newBytes);
            }
        }

        [MenuItem("Tools/File Splitting/Split Files")]
        private static void SplitFiles()
        {
            TryGetSplitSaveFile().DoSplitFiles();
        }

        private void DeleteSplittedFiles(string splitFolderPath)
        {
            //Delete old split files
            Directory.CreateDirectory(@splitFolderPath);
            System.IO.DirectoryInfo dic = new(splitFolderPath);

            foreach (FileInfo file in dic.GetFiles())
            {
                file.Delete();
            }

            splittedFiles.Clear();
            SaveChanges();
        }

        private string GetProjectBasePath()
        {
            string basePath = @Application.dataPath;
            return basePath.Replace("/Assets", string.Empty);
        }

        /// <summary>
        /// Returns the full path to the .gitignore file, returns null if no .gitignore file exist
        /// </summary>
        private string TryGetGitIgnoreFullPath(out string splitFolderFullPath)
        {
            string appPath = GetProjectBasePath();
            string fullIgnorePath = gitIgnorePath == null ? string.Empty : Path.Combine(appPath, gitIgnorePath);
            fullIgnorePath = fullIgnorePath.Replace("\\", "/");

            if (gitIgnorePath == null || gitIgnorePath.Length < 10 || File.Exists(fullIgnorePath) == false)
            {
                //No valid .gitignore is assigned
                DirectoryInfo dicToSearch = new(@appPath);
                FileInfo[] filesInDir = dicToSearch.GetFiles(".gitignore", SearchOption.AllDirectories);

                foreach (FileInfo foundFile in filesInDir)
                {
                    string ignFullPath = @foundFile.FullName;

                    gitIgnorePath = Path.GetRelativePath(appPath, ignFullPath);
                    gitIgnorePath = gitIgnorePath.Replace("\\", "/");

                    fullIgnorePath = Path.Combine(appPath, gitIgnorePath);
                    fullIgnorePath = fullIgnorePath.Replace("\\", "/");

                    if (SplitConfig.requiredGitIgnoreFolderName != null && SplitConfig.requiredGitIgnoreFolderName.Count > 0
                        && SplitConfig.requiredGitIgnoreFolderName.Contains(Path.GetFileName(Path.GetDirectoryName(fullIgnorePath))) == false)
                    {
                        gitIgnorePath = null;
                        continue;
                    }


                    SaveChanges();
                    break;
                }

                if (gitIgnorePath == null || gitIgnorePath.Length < 10 || File.Exists(fullIgnorePath) == false)
                {
                    //No valid .gitignore is found
                    Debug.LogError("Cant split or merge files because no valid .gitignore file was found in " + appPath + " or any of its sub folders");
                    splitFolderFullPath = null;
                    return null;
                }
            }

            splitFolderFullPath = fullIgnorePath.Replace("/.gitignore", "/xSplittedFiles_TEMPONLY_hf4n~");
            return fullIgnorePath;
        }

        private string GetFullOgGitIgnorePath(string fullGitIgnorePath)
        {
            return fullGitIgnorePath.Replace("/.gitignore", "/ogGitIgnore.zombIgnore~");
        }

        private void DoSplitFiles()
        {
            //Restore .gitIgnore
            RestoreGitIgnore();

            //Backup the .gitignore file
            gitIgnorePath = null;//I think its better to always update it
            string fullIgnorePath = TryGetGitIgnoreFullPath(out string splitFolderPath);
            if (fullIgnorePath == null) return;

            File.WriteAllBytes(GetFullOgGitIgnorePath(fullIgnorePath), File.ReadAllBytes(fullIgnorePath));

            //Delete previous temp spilt files
            DeleteSplittedFiles(splitFolderPath);

            //Split files
            long minSizeInBytes = SplitConfig.splitFilesLargerThanMB * 1000000;
            int splitFileNumber = 0;
            string appPath = GetProjectBasePath();
            List<string> gitIgnorePaths = new();

            foreach (string filePath in GetPathToAllFiles(out gitIgnorePaths, splitFolderPath))
            {
                DoSplitFile(filePath);
            }

            SaveChanges();

            //Did we split anything?
            if (splittedFiles.Count == 0)
            {
                RestoreGitIgnore();
                Debug.Log("Got no files to split");
                return;
            }

            //Add splitted files to .gitignore
            gitIgnorePaths.Insert(0, "# Auto generated by unityFileSplitter. NOTE, changes made to this .gitignore will be overwritten when you merge or split files");

            string textToAppend = Environment.NewLine + string.Join(Environment.NewLine, gitIgnorePaths);
            File.AppendAllText(fullIgnorePath, textToAppend);

            //Log result
            Debug.Log("Splitted " + splittedFiles.Count + " files");

            void DoSplitFile(string filePath)
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                long bytesLength = bytes.LongLength;
                long byteI = 0;
                string splitFileNameBase = Path.GetFileName(filePath) + "__";
                List<string> splitFullPaths = new();

                while (true)
                {
                    string splitFileName = splitFileNameBase + splitFileNumber + ".zombSplit";
                    string splitFilePath = Path.Combine(splitFolderPath, splitFileName);

                    File.WriteAllBytes(splitFilePath, GetSplitBytes());
                    splitFullPaths.Add(splitFileName);

                    splitFileNumber++;
                    if (byteI >= bytesLength) break;
                }

                splittedFiles.Add(new()
                {
                    splitFileNames = splitFullPaths,
                    sourceRelativePath = filePath,
                    sourceByteSize = bytesLength
                });

                byte[] GetSplitBytes()
                {
                    long nextByteI = byteI + minSizeInBytes;
                    if (nextByteI > bytesLength) nextByteI = bytesLength;

                    long loopCount = nextByteI - byteI;
                    byte[] splitBytes = new byte[loopCount];

                    for (long i = 0; i < loopCount; i++)
                    {
                        splitBytes[i] = bytes[byteI];
                        byteI++;
                    }

                    return splitBytes;
                }
            }
        }

        private void SaveChanges()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        private List<string> GetPathToAllFiles(out List<string> gitIgnoreFilePaths, string splitFolderPath)
        {
            gitIgnoreFilePaths = new(64);
            List<string> filePaths = new(64);
            long minSizeInBytes = SplitConfig.splitFilesLargerThanMB * 1000000;

            string searchFolderPath = Path.GetRelativePath(GetProjectBasePath(), splitFolderPath);
            searchFolderPath = searchFolderPath.Replace("\\", "/");
            searchFolderPath = searchFolderPath.Replace("/xSplittedFiles_TEMPONLY_hf4n~", string.Empty);

            foreach (string filePath in Directory.GetFiles(splitFolderPath.Contains("/Assets") == false ? "Assets" : searchFolderPath, "*.*", SearchOption.AllDirectories))
            {
                //Ignore file extensions
                if (SplitConfig.fileExtensionsToExclude.Contains(Path.GetExtension(filePath)) == true) continue;

                //Ignore too small files
                string fullPath = Path.GetFullPath(filePath);
                var fData = new FileInfo(fullPath);

                if (fData.Length < minSizeInBytes) continue;
                if (fData.Exists == false) continue;

                //Ignore dictoraries
                bool ignore = false;

                foreach (string dic in SplitConfig.dictorariesToExclude)
                {
                    if (filePath.Contains(dic) == true)
                    {
                        ignore = true;
                        break;
                    }
                }

                if (ignore == true) continue;

                //Add path to list
                string theFilePath = filePath.Replace("\\", "/");
                gitIgnoreFilePaths.Add(theFilePath.Replace(searchFolderPath + "/", string.Empty));
                filePaths.Add(theFilePath);
            }

            return filePaths;
        }

        /// <summary>
        /// Returns the Splitter asset, returns null if it has been deleted
        /// </summary>
        public static FileSplitter TryGetSplitSaveFile()
        {
            FileSplitter splitter = Resources.Load<FileSplitter>("zombFileSplitterData");
            if (splitter == null)
            {
                Debug.LogError("Expected zombFileSplitterData.asset to exist at path UnityFileSplitter/Resources/zombFileSplitterData.asset, have you deleted it?");
                return null;
            }

            return splitter;
        }
    }

    [CustomEditor(typeof(FileSplitter))]
    public class FractureSaveAssetEditor : Editor
    {
        private bool showFloatVariable = false;

        public override void OnInspectorGUI()
        {
            // Show the button to toggle the float variable
            if (GUILayout.Button("Show Splitted Files (MAY FREEZE UNITY!)"))
            {
                showFloatVariable = !showFloatVariable;
            }

            if (showFloatVariable)
            {
                // Show the variables
                serializedObject.Update(); // Ensure serialized object is up to date

                DrawPropertiesExcluding(serializedObject, "m_Script");
            }

            // Apply modifications to the asset
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}
#endif

