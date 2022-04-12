using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;

namespace Tekly.ZipFile
{
    public class ZipFile : IDisposable
    {
        private ZipOutputStream m_zipOutputStream;
        private byte[] m_buffer;
        private const int BUFFER_SIZE = 4096;

        public ZipFile(Stream outputStream, int compressionLevel = 3)
        {
            m_zipOutputStream = new ZipOutputStream(outputStream, BUFFER_SIZE);
            m_zipOutputStream.SetLevel(compressionLevel);
            m_zipOutputStream.UseZip64 = UseZip64.Off;
        }

        public void Finish()
        {
            m_zipOutputStream.Finish();
            m_zipOutputStream.Close();
            m_zipOutputStream.Dispose();

            m_zipOutputStream = null;
        }

        public void Dispose()
        {
            if (m_zipOutputStream != null) {
                m_zipOutputStream.Finish();
                m_zipOutputStream.Close();
                m_zipOutputStream?.Dispose();
            }
        }

        public void AddAsJson(object obj, string entryName)
        {
            AddEntry(JsonUtility.ToJson(obj, true), entryName);
        }

        public void AddEntry(string data, string entryName)
        {
            entryName = ZipEntry.CleanName(entryName);

            var byteLength = Encoding.UTF8.GetByteCount(data);

            var newEntry = new ZipEntry(entryName) {
                DateTime = DateTime.Now,
                Size = byteLength
            };

            m_zipOutputStream.PutNextEntry(newEntry);
            
            using (var sw = new StreamWriter(m_zipOutputStream, new UTF8Encoding(false), BUFFER_SIZE, true)) {
                sw.Write(data);
            }

            m_zipOutputStream.CloseEntry();
        }

        public void AddEntry(byte[] bytes, string entryName)
        {
            entryName = ZipEntry.CleanName(entryName);

            var newEntry = new ZipEntry(entryName) {
                DateTime = DateTime.Now,
                Size = bytes.Length
            };

            m_zipOutputStream.PutNextEntry(newEntry);

            m_zipOutputStream.Write(bytes, 0, bytes.Length);

            m_zipOutputStream.CloseEntry();
        }

        public void AddFile(string filePath, string entryName)
        {
            var fi = new FileInfo(filePath);

            entryName = ZipEntry.CleanName(entryName);

            var newEntry = new ZipEntry(entryName) {
                DateTime = fi.LastWriteTime,
                Size = fi.Length
            };

            m_zipOutputStream.PutNextEntry(newEntry);

            var buffer = GetBuffer();
            using (var fsInput = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                StreamUtils.Copy(fsInput, m_zipOutputStream, buffer);
            }

            m_zipOutputStream.CloseEntry();
        }

        private byte[] GetBuffer()
        {
            return m_buffer ??= new byte[BUFFER_SIZE];
        }
    }
}