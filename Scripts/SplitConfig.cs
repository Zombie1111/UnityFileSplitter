//Made by David Westberg
#if UNITY_EDITOR
using System.Collections.Generic;

namespace zombFiles
{
    public static class SplitConfig
    {
        public static readonly string[] dictorariesToIgnore = new string[] { };
        public static readonly HashSet<string> fileExtensionsToIgnore = new() { ".meta", ".cs" };
        public const int splitFilesLargerThanMB = 99;
        public const string requiredGitIgnoreFolderName = null;//If assigned, the .gitIgnore file must be in a folder with this name
    }
}
#endif
