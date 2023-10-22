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
        private StreamWriter m_streamWriter;
        private AutoResetEvent m_newLogEvent = new AutoResetEvent(false);
        private volatile bool m_resetQueued;

        private readonly ConcurrentQueue<TkLogMessage> m_messages = new ConcurrentQueue<TkLogMessage>();
        private readonly StringBuilder m_stringBuilder = new StringBuilder(512);
        private readonly ExpandingBuffer<char> m_expandingBuffer = new ExpandingBuffer<char>(1024);
        private readonly Thread m_thread;
        
        private bool m_disposing;
        private bool m_disposed;

        protected FileLogDestination(FileLogConfig config)
        {
            Name = config.Name;
            
            CurrentFilePath = $"logs/{config.Prefix}_curr.log";
            PrevFilePath = $"logs/{config.Prefix}_prev.log";
            
            if (LocalFile.Exists(CurrentFilePath)) {
                LocalFile.Rename(CurrentFilePath, PrevFilePath);
            }

            m_streamWriter = LocalFile.GetStreamWriter(CurrentFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            m_streamWriter.AutoFlush = true;
            m_minimumLevel = config.MinimumLevel;

            m_thread = StartLongLivingThread(WriteMessageLoop);
        }

        ~FileLogDestination()
        {
            if (m_disposing || m_disposed) {
                return;
            }
            
            Dispose();
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
                m_newLogEvent.WaitOne();

                if (m_resetQueued) {
                    m_resetQueued = false;
                    Reset();
                }

                while (m_messages.TryDequeue(out var logMessage)) {
                    Write(logMessage);
                }
            } while (!m_disposing);
        }

        private void Reset()
        {
            if (m_disposing) {
                return;
            }

            m_streamWriter.Dispose();
            LocalFile.Delete(CurrentFilePath);
            
            m_streamWriter = LocalFile.GetStreamWriter(CurrentFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            m_streamWriter.AutoFlush = true;
        }

        private void Write(TkLogMessage logMessage)
        {
            if (m_disposing) {
                return;
            }

            m_stringBuilder.Clear();
            
            ConvertLogMessage(logMessage, m_stringBuilder);
            
            var len = m_stringBuilder.Length;
            m_expandingBuffer.EnsureSize(len);
            m_stringBuilder.CopyTo(0, m_expandingBuffer.Buffer, 0, len);
            m_streamWriter.Write(m_expandingBuffer.Buffer, 0, len);
        }
        
        private Thread StartLongLivingThread(Action action)
        {
            var thread = new Thread(param => action()) {
                Name = Name
            };
            
            thread.Start();

            return thread;
        }

        public void Dispose()
        {
            if (m_disposing) {
                return;
            }

            m_disposing = true;
            
            m_newLogEvent.Set();
            m_thread.Join();

            m_streamWriter.Dispose();
            m_streamWriter = null;

            m_newLogEvent.Close();
            m_newLogEvent = null;

            m_disposed = true;
        }
    }
}