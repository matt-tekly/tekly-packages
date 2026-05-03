using System.IO;
using System.Linq;
using UnityEngine;

namespace Tekly.Common.LocalFiles
{
    public static class LocalFile
    {
        public static readonly string Directory;
        private const string LocalFileDir = "LocalFiles";
        
        static LocalFile()
        {
            if (Application.isEditor) {
                Directory = LocalFileDir;
                System.IO.Directory.CreateDirectory(Directory);
            } else if (Application.platform == RuntimePlatform.tvOS) {
                Directory = Application.temporaryCachePath;
            } else {
                Directory = Application.persistentDataPath;
            }
        }

        public static string GetPath(string file)
        {
            return Path.Combine(Directory, file);
        }
        
        public static string GetFullPath(string file)
        {
            return Path.GetFullPath(Path.Combine(Directory, file));
        }

        public static string[] GetFiles(string directory, string search, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var directoryPath = GetPath(directory);
            var prefixLength = GetPath("").Length;
            return System.IO.Directory.GetFileSystemEntries(directoryPath, search, searchOption)
                .Select(x => x.Substring(prefixLength))
                .ToArray();;
        }
        
        public static string[] GetFiles(string directory, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var directoryPath = GetPath(directory);
            var prefixLength = GetPath("").Length;
            return System.IO.Directory.GetFileSystemEntries(directoryPath, "*", searchOption)
                .Select(x => x.Substring(prefixLength))
                .ToArray();
        }

        public static string ReadAllText(string relativeFile)
        {
            var filePath = GetPath(relativeFile);
            return File.ReadAllText(filePath);
        }
        
        public static bool TryReadAllText(string relativeFile, out string text)
        {
            var filePath = GetPath(relativeFile);
            if (File.Exists(filePath)) {
                text = File.ReadAllText(filePath);
                return true;
            }
            
            text = null;
            return false;
        }
        
        public static byte[] ReadAllBytes(string relativeFile)
        {
            var filePath = GetPath(relativeFile);
            return File.ReadAllBytes(filePath);
        }
        
        public static bool TryReadAllBytes(string relativeFile, out byte[] bytes)
        {
            var filePath = GetPath(relativeFile);
            if (File.Exists(filePath)) {
                bytes = File.ReadAllBytes(filePath);
                return true;
            }
            
            bytes = null;
            return false;
        }
        
        public static void WriteAllText(string relativeFile, string text)
        {
            var filePath = GetPath(relativeFile);
            EnsureDirectoryExistsForFile(filePath);
            File.WriteAllText(filePath, text);
        }
        
        public static void WriteAllBytes(string relativeFile, byte[] bytes)
        {
            var filePath = GetPath(relativeFile);
            EnsureDirectoryExistsForFile(filePath);
            File.WriteAllBytes(filePath, bytes);
        }

        public static bool Exists(string relativeFile)
        {
            var filePath = GetPath(relativeFile);
            return File.Exists(filePath);
        }

        public static void Delete(string relativeFile)
        {
            if (Exists(relativeFile)) {
                var filePath = GetPath(relativeFile);
                File.Delete(filePath);    
            }
        }
        
        public static void DeleteDirectory(string relativeFile)
        {
            var dir = new DirectoryInfo(GetPath(relativeFile));

            if (dir.Exists) {
                dir.Delete(true);
            }
        }

        private static void EnsureDirectoryExistsForFile(string file)
        {
            var directoryInfo = new FileInfo(file).Directory;
            directoryInfo?.Create();
        }

        public static FileStream WriteStream(string relativeFile, FileAccess fileAccess = FileAccess.Write)
        {
            var filePath = GetPath(relativeFile);
            EnsureDirectoryExistsForFile(filePath);
            
            return new FileStream(filePath, FileMode.Create, fileAccess);
        }
        
        public static FileStream GetStream(string relativeFile, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            var filePath = GetPath(relativeFile);
            EnsureDirectoryExistsForFile(filePath);
            
            return new FileStream(filePath, fileMode, fileAccess, fileShare);
        }

        public static StreamWriter GetStreamWriter(string relativeFile, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            var filePath = GetPath(relativeFile);
            EnsureDirectoryExistsForFile(filePath);

            var fs = new FileStream(filePath, fileMode, fileAccess, fileShare);
            return new StreamWriter(fs);
        }

        public static void Rename(string sourceName, string destinationName)
        {
            var sourceFilePath = GetPath(sourceName);
            var destinationFilePath = GetPath(destinationName);

            if (File.Exists(destinationFilePath)) {
                File.Delete(destinationFilePath);
            }
            
            File.Move(sourceFilePath, destinationFilePath);
        }

        public static long GetSize(string filePath)
        {
            var localFilePath = GetPath(filePath);
            var info = new FileInfo(localFilePath);
            return info.Exists ? info.Length : 0;
        }
    }
}