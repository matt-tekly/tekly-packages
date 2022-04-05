using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using Tekly.Common.Utils;
using Object = UnityEngine.Object;

namespace Tekly.Logging.LogDestinations
{
    public abstract class FileLogDestination : ITkLogDestination
    {
        private readonly TkLogLevel m_minimumLevel;
        private readonly Stream m_fileStream;

        private readonly ConcurrentQueue<TkLogMessage> m_messages = new ConcurrentQueue<TkLogMessage>();
        private readonly AutoResetEvent m_newLogEvent = new AutoResetEvent(false);
        private readonly StringBuilder m_stringBuilder = new StringBuilder(512);
        private readonly ExpandingBuffer m_expandingBuffer = new ExpandingBuffer(1024);

        protected FileLogDestination(string fileName, TkLogLevel minimumLevel) 
            : this(new FileStream(fileName, FileMode.Create), minimumLevel)
        {
        }

        protected FileLogDestination(Stream fileStream, TkLogLevel minimumLevel)
        {
            m_minimumLevel = minimumLevel;
            m_fileStream = fileStream;

            StartLongLivingThread(WriteMessageLoop);
        }
        
        public void LogMessage(TkLogMessage message)
        {
            if (message.Level < m_minimumLevel) {
                return;
            }
            
            m_messages.Enqueue(message);
            m_newLogEvent.Set();
        }

        public void LogMessage(TkLogMessage message, Object context)
        {
            LogMessage(message);
        }

        public void Update() { }

        private void WriteMessageLoop()
        {
            do {
                m_newLogEvent.WaitOne();

                while (m_messages.TryDequeue(out var logMessage)) {
                    Write(logMessage);
                }
            } while (true);
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

        protected abstract void ConvertLogMessage(TkLogMessage message, StringBuilder sb);

        private static void StartLongLivingThread(Action action)
        {
            var thread = new Thread(param => action());
            thread.Start();
        }
    }
}