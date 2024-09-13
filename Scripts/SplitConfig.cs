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
        public static readonly HashSet<string> requiredGitIgnoreFolderName = new() {  };//If set, the .gitIgnore file must be in a folder with any of these name
    }
}
#endif
