using System;
using System.IO;
using UnityEngine;

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
        
        public static void RevealInFileBrowser(string path)
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer) {
                RevealInExplorer(path);
            } else if (Application.platform == RuntimePlatform.OSXPlayer) {
                RevealInFinder(path);
            } else {
                throw new Exception("Platform doesn't support revealing files in file browser");
            }
        }

        private static void RevealInFinder(string path)
        {
            path = path.Replace("\\", "/");
            var isFolder = Directory.Exists(path);
            var arguments = (isFolder ? "" : "-R ") + $"\"{path}\"";
            System.Diagnostics.Process.Start("open", arguments);
        }

        private static void RevealInExplorer(string path)
        {
            path = path.Replace("/", "\\");
            var isFolder = Directory.Exists(path);
            System.Diagnostics.Process.Start("explorer.exe", (isFolder ? "/root," : "/select,") + path);
        }
    }
}