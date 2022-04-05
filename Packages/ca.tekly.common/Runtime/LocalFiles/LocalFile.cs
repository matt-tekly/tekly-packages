// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System.IO;
using System.Linq;
using UnityEngine;

namespace Tekly.Common.LocalFiles
{
    public static class LocalFile
    {
        private static readonly string s_directory;
        private const string LocalFileDir = "LocalFiles";
        
        static LocalFile()
        {
            if (Application.isEditor) {
                s_directory = LocalFileDir;
                Directory.CreateDirectory(s_directory);
            } else {
                s_directory = Application.persistentDataPath;
            }
        }

        public static string GetPath(string file)
        {
            return Path.Combine(s_directory, file);
        }

        public static string[] GetFiles(string directory, string search, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var directoryPath = GetPath(directory);
            var prefixLength = GetPath("").Length;
            return Directory.GetFileSystemEntries(directoryPath, search, searchOption)
                .Select(x => x.Substring(prefixLength))
                .ToArray();;
        }
        
        public static string[] GetFiles(string directory, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var directoryPath = GetPath(directory);
            var prefixLength = GetPath("").Length;
            return Directory.GetFileSystemEntries(directoryPath, "*", searchOption)
                .Select(x => x.Substring(prefixLength))
                .ToArray();
        }

        public static string ReadAllText(string relativeFile)
        {
            var filePath = GetPath(relativeFile);
            return File.ReadAllText(filePath);
        }
        
        public static byte[] ReadAllBytes(string relativeFile)
        {
            var filePath = GetPath(relativeFile);
            return File.ReadAllBytes(filePath);
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

        private static void EnsureDirectoryExistsForFile(string file)
        {
            var directoryInfo = new FileInfo(file).Directory;
            directoryInfo?.Create();
        }

        public static FileStream GetStream(string relativeFile, FileMode fileMode)
        {
            var filePath = GetPath(relativeFile);
            EnsureDirectoryExistsForFile(filePath);
            
            return new FileStream(filePath, fileMode);
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
    }
}