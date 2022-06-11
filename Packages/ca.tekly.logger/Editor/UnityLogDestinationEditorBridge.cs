using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Tekly.Logging.LogDestinations
{
    public static class UnityLogDestinationEditorBridge
    {
        public static void Log(LogType logType, string message)
        {
            var logEntry = ParseMessage(message);

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