using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using Tekly.Common.LocalFiles;
using Tekly.Common.Utils;
using Object = UnityEngine.Object;

namespace Tekly.Logging.LogDestinations
{
    public abstract class FileLogConfig : LogDestinationConfig
    {
        [XmlAttribute] public string Prefix;
        [XmlAttribute] public TkLogLevel MinimumLevel;
    }
    
    public abstract class FileLogDestination : ILogDestination
    {
        public string Name { get; }
        public string CurrentFilePath { get; }
        public string PrevFilePath { get; }
        
        private readonly TkLogLevel m_minimumLevel;
        private Stream m_fileStream;
        private AutoResetEvent m_newLogEvent = new AutoResetEvent(false);
        private volatile bool m_resetQueued;
        
        private readonly ConcurrentQueue<TkLogMessage> m_messages = new ConcurrentQueue<TkLogMessage>();
        private readonly StringBuilder m_stringBuilder = new StringBuilder(512);
        private readonly ExpandingBuffer m_expandingBuffer = new ExpandingBuffer(1024);
        
        protected FileLogDestination(FileLogConfig config)
        {
            Name = config.Name;
            
            CurrentFilePath = $"logs/{config.Prefix}_curr.log";
            PrevFilePath = $"logs/{config.Prefix}_prev.log";
            
            if (LocalFile.Exists(CurrentFilePath)) {
                LocalFile.Rename(CurrentFilePath, PrevFilePath);
            }
            
            m_fileStream = LocalFile.GetStream(CurrentFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            m_minimumLevel = config.MinimumLevel;

            StartLongLivingThread(WriteMessageLoop);
        }

        public void LogMessage(TkLogMessage message, LogSource logSource)
        {
            if (message.Level < m_minimumLevel) {
                return;
            }
            
            m_messages.Enqueue(message);
            m_newLogEvent?.Set();
        }

        public void LogMessage(TkLogMessage message, Object context, LogSource logSource)
        {
            LogMessage(message, logSource);
        }

        public void QueueReset()
        {
            m_resetQueued = true;
            m_newLogEvent?.Set();
        }

        public void Update() { }
        
        protected abstract void ConvertLogMessage(TkLogMessage message, StringBuilder sb);

        private void WriteMessageLoop()
        {
            do {
                m_newLogEvent?.WaitOne();

                if (m_resetQueued) {
                    m_resetQueued = false;
                    Reset();
                }

                while (m_messages.TryDequeue(out var logMessage)) {
                    Write(logMessage);
                }
            } while (true);
        }

        private void Reset()
        {
            m_fileStream?.Dispose();

            LocalFile.Delete(CurrentFilePath);
            
            m_fileStream = LocalFile.GetStream(CurrentFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        }

        private void Write(TkLogMessage logMessage)
        {
            if (m_fileStream == null) {
                return;
            }

            m_stringBuilder.Clear();
            
            ConvertLogMessage(logMessage, m_stringBuilder);
            
            var logString = m_stringBuilder.ToString();

            var count = Encoding.UTF8.GetByteCount(logString);
            m_expandingBuffer.EnsureSize(count);

            Encoding.UTF8.GetBytes(logString, 0, logString.Length, m_expandingBuffer.Buffer, 0);
            m_fileStream.Write(m_expandingBuffer.Buffer, 0, count);

            m_fileStream.Flush();
        }
        
        private static void StartLongLivingThread(Action action)
        {
            var thread = new Thread(param => action());
            thread.Start();
        }

        public void Dispose()
        {
            m_fileStream?.Dispose();
            m_newLogEvent?.Dispose();

            m_fileStream = null;
            m_newLogEvent = null;
        }
    }
}