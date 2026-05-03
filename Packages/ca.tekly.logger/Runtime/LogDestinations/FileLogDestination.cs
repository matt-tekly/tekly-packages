using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using Tekly.Common.LocalFiles;
using Tekly.Common.Utils;
using Object = UnityEngine.Object;

namespace Tekly.Logging.LogDestinations
{
    public abstract class FileLogDestination : ILogDestination
    {
        public string Name { get; }
        public string CurrentFilePath { get; }
        public string PrevFilePath { get; }

        private readonly TkLogLevel m_minimumLevel;
        private StreamWriter m_streamWriter;
        private AutoResetEvent m_newLogEvent = new AutoResetEvent(false);

        private volatile bool m_resetQueued;
        private volatile bool m_disposing;

        private readonly ConcurrentQueue<TkLogMessage> m_messages = new ConcurrentQueue<TkLogMessage>();
        private readonly StringBuilder m_stringBuilder = new StringBuilder(512);
        private readonly ExpandingBuffer<char> m_expandingBuffer = new ExpandingBuffer<char>(1024);
        private readonly Thread m_thread;

        protected FileLogDestination(string name, string prefix, TkLogLevel minimumLevel)
        {
            Name = name;
            CurrentFilePath = $"logs/{prefix}_curr.log";
            PrevFilePath = $"logs/{prefix}_prev.log";
            m_minimumLevel = minimumLevel;

            try {
                RotateLog();

                m_streamWriter = LocalFile.GetStreamWriter(CurrentFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                m_streamWriter.AutoFlush = true;

                m_thread = StartLongLivingThread(WriteMessageLoop);
            } catch {
                // Make sure we don't leak the writer or the wait handle if construction fails partway.
                m_streamWriter?.Dispose();
                m_streamWriter = null;
                m_newLogEvent?.Close();
                m_newLogEvent = null;
                throw;
            }
        }

        private void RotateLog()
        {
            if (!LocalFile.Exists(CurrentFilePath)) {
                return;
            }
            
            if (LocalFile.GetSize(CurrentFilePath) == 0) {
                LocalFile.Delete(CurrentFilePath);
                return;
            }
            
            LocalFile.Delete(PrevFilePath);
            LocalFile.Rename(CurrentFilePath, PrevFilePath);
        }

        public void LogMessage(TkLogMessage message, LogSource logSource)
        {
            if (m_disposing) {
                return;
            }

            if (message.Level < m_minimumLevel) {
                return;
            }

            m_messages.Enqueue(message);
            SignalWorker();
        }

        public void LogMessage(TkLogMessage message, Object context, LogSource logSource)
        {
            LogMessage(message, logSource);
        }

        public void QueueReset()
        {
            if (m_disposing) {
                return;
            }

            m_resetQueued = true;
            SignalWorker();
        }

        public void Update() { }

        protected abstract void ConvertLogMessage(TkLogMessage message, StringBuilder sb);

        private void SignalWorker()
        {
            // The wait handle can be closed concurrently by Dispose between our null check and Set.
            try {
                m_newLogEvent?.Set();
            } catch (ObjectDisposedException) { }
        }

        private void WriteMessageLoop()
        {
            while (!m_disposing) {
                m_newLogEvent.WaitOne();

                if (m_resetQueued) {
                    m_resetQueued = false;
                    Reset();
                }

                DrainQueue();
            }

            // Final drain so messages enqueued just before Dispose are not lost.
            DrainQueue();
        }

        private void DrainQueue()
        {
            while (m_messages.TryDequeue(out var logMessage)) {
                Write(logMessage);
            }
        }

        private void Reset()
        {
            m_streamWriter.Dispose();
            LocalFile.Delete(CurrentFilePath);

            m_streamWriter = LocalFile.GetStreamWriter(CurrentFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            m_streamWriter.AutoFlush = true;
        }

        private void Write(TkLogMessage logMessage)
        {
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
                Name = Name,
                IsBackground = true
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

            // Wake the worker so it observes m_disposing and runs its final drain.
            SignalWorker();

            m_thread?.Join();

            m_streamWriter?.Dispose();
            m_streamWriter = null;

            m_newLogEvent?.Close();
            m_newLogEvent = null;
        }
    }
}