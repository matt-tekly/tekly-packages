using System.IO;

namespace Tekly.Common.Utils
{
    public static class FileUtils
    {
        public static void SafeDeleteDirectory(string path, bool recursive = false)
        {
            if (Directory.Exists(path)) {
                Directory.Delete(path, recursive);
            }
        }
    }
}