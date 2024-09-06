//Made by David Westberg
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace zombFiles
{
    public class FileSplitter : ScriptableObject
    {
        [SerializeField] private List<SplitFile> splittedFiles = new();

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
            TryGetSplitSaveFile().DoMergeFiles();
        }

        [MenuItem("Tools/File Splitting/MX_JustToAddSpacing")]
        [MenuItem("Tools/File Splitting/SA_EvenMoreSpacing")]
        public static void MenuSpace()
        {
            Debug.Log("Just for spacing I said :D");
        }

        private void DoMergeFiles()
        {
            if (splittedFiles.Count == 0)
            {
                Debug.Log("Got no files to merge");
                return;
            }

            foreach (SplitFile file in splittedFiles)
            {
                DoMergeFile(file);
            }

            Debug.Log("Merged " + splittedFiles.Count + " files");
            DeleteSplittedFiles();

            static void DoMergeFile(SplitFile file)
            {
                //Verify all split files exist and get their data
                byte[] newBytes = new byte[file.sourceByteSize];
                long newBytesI = 0;
                string splitFolderPath = Application.dataPath + "/xSplittedFiles_TEMPONLY_hf4n~";

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

        private string DeleteSplittedFiles()
        {
            //Delete old split files
            //string SplitPath = Application.dataPath + "/xSplittedFiles_TEMPONLY_hf4n~";
            string SplitPath = Application.dataPath + "/xSplittedFiles_TEMPONLY_hf4n~";
            Directory.CreateDirectory(@SplitPath);
            System.IO.DirectoryInfo dic = new(SplitPath);

            foreach (FileInfo file in dic.GetFiles())
            {
                file.Delete();
            }

            splittedFiles.Clear();
            SaveChanges();

            return SplitPath;
        }

        private void DoSplitFiles()
        {
            string SplitPath = DeleteSplittedFiles();

            //Split files
            long minSizeInBytes = SplitConfig.splitFilesLargerThanMB * 1000000;
            int splitFileNumber = 0;

            foreach (string filePath in GetPathToAllFiles())
            {
                DoSplitFile(filePath);
            }

            SaveChanges();
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
                    string splitFilePath = Path.Combine(SplitPath, splitFileName);

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

        private static List<string> GetPathToAllFiles()
        {
            List<string> filePaths = new(64);
            long minSizeInBytes = SplitConfig.splitFilesLargerThanMB * 1000000;

            foreach (string filePath in Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories))
            {
                //Ignore file extensions
                if (SplitConfig.fileExtensionsToIgnore.Contains(Path.GetExtension(filePath)) == true) continue;

                //Ignore too small files
                var fData = new FileInfo(Path.GetFullPath(filePath));
                if (fData.Length < minSizeInBytes) continue;
                if (fData.Exists == false) continue;

                //Ignore dictoraries
                bool ignore = false;

                foreach (string dic in SplitConfig.dictorariesToIgnore)
                {
                    if (filePath.Contains(dic) == true)
                    {
                        ignore = true;
                        break;
                    }
                }

                if (ignore == true) continue;

                //Add path to list
                filePaths.Add(filePath);
            }

            return filePaths;
        }

        /// <summary>
        /// Returns the Splitter asset, returns null if it has been deleted
        /// </summary>
        private static FileSplitter TryGetSplitSaveFile()
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

