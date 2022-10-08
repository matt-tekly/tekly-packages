using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace Tekly.ZipFile
{
    public static class ZipUtils
    {
        public static void CompressDirectory(string outputFileName, string directoryPath, int compressionLevel = 3)
        {
            using (var outputStream = File.Create(outputFileName)) {
                CompressDirectory(outputStream, directoryPath, compressionLevel);
            }
        }
        
        public static void CompressDirectory(Stream outputStream, string directoryPath, int compressionLevel = 3)
        {
            using (var zipStream = new ZipOutputStream(outputStream)) {
                zipStream.SetLevel(compressionLevel);
                CompressDirectoryToZip(directoryPath, zipStream);
            }
        }

        public static void CompressDirectoryToZip(string directoryPath, ZipOutputStream zipStream)
        {
            directoryPath = directoryPath.Replace('\\', '/');
            var rootDirectoryOffset = directoryPath.Length + (directoryPath.EndsWith("/") ? 0 : 1);
            CompressDirectoryToZip(directoryPath, zipStream, rootDirectoryOffset);
        }

        public static void CompressDirectoryToZip(string directoryPath, ZipOutputStream zipStream, int rootDirectoryOffset)
        {
            var files = Directory.GetFiles(directoryPath);

            foreach (var filename in files) {
                var fileInfo = new FileInfo(filename);

                // Make the name in zip based on the folder
                var entryName = filename.Substring(rootDirectoryOffset);

                // Remove drive from name and fix slash direction
                entryName = ZipEntry.CleanName(entryName);

                var zipEntry = new ZipEntry(entryName);
                
                zipEntry.DateTime = fileInfo.LastWriteTime;
                
                // To permit the zip to be unpacked by built-in extractor in WinXP and Server2003,
                // WinZip 8, Java, and other older code, you need to do one of the following: 
                // Specify UseZip64.Off, or set the Size.
                // If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, 
                // you do not need either, but the zip will be in Zip64 format which
                // not all utilities can understand.
                //   zipStream.UseZip64 = UseZip64.Off;
                zipEntry.Size = fileInfo.Length;

                zipStream.PutNextEntry(zipEntry);

                // Zip the file in buffered chunks
                // the "using" will close the stream even if an exception occurs
                var buffer = new byte[4096];
                
                using (var fsInput = File.OpenRead(filename)) {
                    StreamUtils.Copy(fsInput, zipStream, buffer);
                }

                zipStream.CloseEntry();
            }

            // Recursively call CompressFolder on all folders in path
            var directories = Directory.GetDirectories(directoryPath);
            foreach (var directory in directories) {
                CompressDirectoryToZip(directory, zipStream, rootDirectoryOffset);
            }
        }
    }
}