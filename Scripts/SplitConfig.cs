//Made by David Westberg
#if UNITY_EDITOR
using System.Collections.Generic;

namespace zombFiles
{
    public static class SplitConfig
    {
        public static readonly string[] dictorariesToExclude = new string[] { };//Files that are in or in any sub folder of a folder that has any of these names will never be splitted

        public static readonly HashSet<string> fileExtensionsToExclude = new() { ".meta" };//Files with these extensions will never be splitted, main usage is to decrease splitting time
                                                                                           //(Add files types that are "garanteed" to never be larger than splitFilesLargerThanMB MB)

        public const int splitFilesLargerThanMB = 99;//If a file is larger that this it gets splitted into files that are smaller than this

        public static readonly HashSet<string> requiredGitIgnoreFolderName = new() {  };//The .gitIgnore file to use must be in a folder with any of these names
    }
}
#endif
