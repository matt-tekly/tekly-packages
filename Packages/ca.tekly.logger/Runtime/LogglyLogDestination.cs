using System.Collections.Concurrent;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Tekly.Logging
{
    /// <summary>
    /// Sends logs to Loggly.
    /// This destination has its own minimum LogLevel that defaults to Error.
    /// </summary>
    public class LogglyLogDestination : ITkLogDestination
    {
        public const string TEMPLATE_URL = "https://logs-01.loggly.com/bulk/{0}/tag/{1}/";
        public readonly string Token;
        public readonly string Url;

        public TkLogLevel MinLevel;
        
        private UnityWebRequestAsyncOperation m_operation;
        
        private float m_timeToLog = 3;
        private float m_timer;

        private readonly ConcurrentQueue<TkLogMessage> m_messageQueue = new ConcurrentQueue<TkLogMessage>();
        private readonly StringBuilder m_allJsonBuilder = new StringBuilder();
        
        public LogglyLogDestination(string token, string tags, TkLogLevel minimumLevel)
        {
            Token = token;
            Url = string.Format(TEMPLATE_URL, Token, tags);
            MinLevel = minimumLevel;
        }

        public void LogMessage(TkLogMessage message)
        {
            if (message.Level >= MinLevel) {
                m_timer = m_timeToLog;
                m_messageQueue.Enqueue(message);
            }
        }

        public void LogMessage(TkLogMessage message, Object context)
        {
            LogMessage(message);
        }
        
        public void Update()
        {
            if (m_messageQueue.Count > 0) {
                m_timer -= Time.unscaledDeltaTime;
                
                if (m_timer <= 0 && m_operation == null) {
                    SendMessageToLoggly();
                }
            }
            
            if (m_operation != null && m_operation.isDone) {
                m_operation = null;
            }
        }

        /// <summary>
        /// Sends messages to Loggly. Each message is converted to JSON and separated by a new line character.
        /// This means each messages needs to have its new line characters converted to "\\n" instead of "\n" or Loggly
        /// will interpret each "\n" as the start of a new log
        /// </summary>
        private void SendMessageToLoggly()
        {
            m_allJsonBuilder.Clear();

            var first = true;
            while (m_messageQueue.TryDequeue(out var logMessage)) {
                if (!first) {
                    m_allJsonBuilder.Append("\n");
                }

                first = false;

                var startIndex = m_allJsonBuilder.Length;
                logMessage.ToJson(m_allJsonBuilder);

                m_allJsonBuilder.Replace("\\", "/", startIndex, m_allJsonBuilder.Length - startIndex);
                m_allJsonBuilder.Replace("\n", "\\n", startIndex, m_allJsonBuilder.Length - startIndex);
            }

            var request = new UnityWebRequest(Url, "POST");
            var bytes = Encoding.UTF8.GetBytes(m_allJsonBuilder.ToString());
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            m_operation = request.SendWebRequest();
        }
    }
}