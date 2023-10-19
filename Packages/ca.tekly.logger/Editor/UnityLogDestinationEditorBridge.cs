using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Tekly.Logging.LogDestinations
{
    public static class UnityLogDestinationEditorBridge
    {
        private struct LogMessage
        {
            public LogEntry Entry;
            public LogType LogType;
        }

        private static ConcurrentQueue<LogMessage> s_queuedMessages = new ConcurrentQueue<LogMessage>();
        private static Thread s_mainThread;
        private static bool s_isUpdateQueued;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            s_mainThread = Thread.CurrentThread;
        }
        
        public static void Log(LogType logType, string message)
        {
            var logEntry = ParseMessage(message);
            
            if (Thread.CurrentThread == s_mainThread) {
                Log(logType, logEntry);    
            } else {
                if (!s_isUpdateQueued) {
                    EditorApplication.delayCall += Update;
                    s_isUpdateQueued = true;
                }
                
                s_queuedMessages.Enqueue(new LogMessage {
                    Entry = logEntry,
                    LogType =  logType
                });
            }
        }

        public static void Update()
        {
            s_isUpdateQueued = false;
            
            while (s_queuedMessages.TryDequeue(out var queuedMessage)) {
                Log(queuedMessage.LogType, queuedMessage.Entry);
            }
        }

        private static void Log(LogType logType, LogEntry logEntry)
        {
            switch (logType) {
                case LogType.Log:
                    LogInfo(logEntry);
                    break;
                case LogType.Warning:
                    LogWarning(logEntry);
                    break;
                default:
                    LogError(logEntry);
                    break;
            }
        }

        private static LogEntry ParseMessage(string message)
        {
            var match = Regex.Match(message, "<a href=\"(.*)\" line=\"([0-9]+)");

            var output = new LogEntry {
                message = message
            };

            if (match != Match.Empty && match.Groups.Count > 2) {
                output.file = match.Groups[1].Captures[0].Value;
                output.line = Convert.ToInt32(match.Groups[2].Captures[0].Value);
            }

            return output;
        }

        private static void LogInfo(LogEntry logEntry)
        {
            Debug.LogInfo(logEntry.message, logEntry.file, logEntry.line, logEntry.column);
        }

        private static void LogWarning(LogEntry logEntry)
        {
            Debug.LogWarning(logEntry.message, logEntry.file, logEntry.line, logEntry.column);
        }

        private static void LogError(LogEntry logEntry)
        {
            Debug.LogError(logEntry.message, logEntry.file, logEntry.line, logEntry.column);
        }
    }
}