﻿using System;
using System.Text;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using System.Text.RegularExpressions;
#endif

namespace Tekly.Logging.LogDestinations
{
    public class UnityLogDestination : ILogDestination
    {
        public string Name { get; }

        private readonly LogPrefixes[] m_prefixes;
        private int m_currentFrame;

        private readonly ThreadLocal<StringBuilder> m_stringBuilders = new ThreadLocal<StringBuilder>(() => new StringBuilder(512));

#if UNITY_EDITOR
        private Regex m_linkRegex = new Regex("(.* \\(at )(([^\\/].*):([0-9]+))", RegexOptions.RightToLeft);
#endif

        public UnityLogDestination(string name, LogPrefixes[] prefixes)
        {
            m_prefixes = prefixes;
            Name = name;
        }

        public void Dispose() { }

        public void LogMessage(TkLogMessage message, LogSource logSource)
        {
            LogMessage(message, null, logSource);
        }

        public void LogMessage(TkLogMessage message, Object context, LogSource logSource)
        {
            if (logSource == LogSource.Unity) {
                return;
            }

            var sb = m_stringBuilders.Value;
            sb.Clear();

            for (var i = 0; i < m_prefixes.Length; i++) {
                switch (m_prefixes[i]) {
                    case LogPrefixes.Logger:
                        sb.Append("[");
                        sb.Append(message.LoggerName);
                        sb.Append("]");
                        break;
                    case LogPrefixes.Frame:
                        sb.Append("[");
                        sb.Append(m_currentFrame);
                        sb.Append("]");
                        break;
                    case LogPrefixes.Level:
                        sb.Append(TkLoggerUtils.LevelToCharacter(message.Level));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (m_prefixes.Length > 0) {
                sb.Append(" ");
            }
            
            message.Print(sb);

            sb.Append("\n\n");

            bool foundException = false;

            if (message.Params != null) {
                foreach (var messageParam in message.Params) {
                    if (string.Equals(messageParam.Id, LoggerConstants.EXCEPTION_MESSAGE_KEY)) {
                        foundException = true;
                        sb.Append(messageParam.Value);
                    }

                    if (string.Equals(messageParam.Id, LoggerConstants.EXCEPTION_STACKTRACE_KEY)) {
                        sb.Append("\n");
                        sb.Append(messageParam.Value);
                    }
                }
            }

            if (!foundException) {
#if UNITY_EDITOR
                sb.Append(LinkStacktraceToCode(message.StackTrace));
#else
                sb.Append(message.StackTrace);
#endif
            }

            sb.Append(LoggerConstants.UNITY_LOG_MARKER);

            try {
                var logType = LevelToType(message.Level);
#if UNITY_EDITOR
                if (context == null) {
                    UnityLogDestinationEditorBridge.Log(logType, sb.ToString());
                } else {
                    Debug.LogFormat(logType, LogOption.NoStacktrace, context, sb.ToString());
                }
#else
                Debug.LogFormat(logType, LogOption.NoStacktrace, context, sb.ToString());
#endif
                    
            } catch (Exception ex) {
                Debug.LogError("Exception while trying to log a message. Likely one of its params is not set. Message:\n\n" + sb, context);
                Debug.LogException(ex, context);
            }
        }

#if UNITY_EDITOR
        private string LinkStacktraceToCode(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) {
                return stackTrace;
            }

            var sb = new StringBuilder(stackTrace.Length * 2);
            var rows = stackTrace.Split('\n');

            foreach (var row in rows) {
                var match = m_linkRegex.Match(row);

                if (!match.Success) {
                    sb.Append(row).Append("\n");
                    continue;
                }

                sb.Append(match.Groups[1].Value)
                    .Append("<a href=\"")
                    .Append(match.Groups[3].Value)
                    .Append("\" line=\"")
                    .Append(match.Groups[4].Value)
                    .Append("\">")
                    .Append(match.Groups[2].Value)
                    .Append("</a>)\n");
            }

            return sb.ToString();
        }
#endif

        public void Update()
        {
            m_currentFrame = Time.frameCount;

#if UNITY_EDITOR
            UnityLogDestinationEditorBridge.Update();
#endif
        }

        public static LogType LevelToType(TkLogLevel level)
        {
            switch (level) {
                case TkLogLevel.Debug:
                    return LogType.Log;
                case TkLogLevel.Info:
                    return LogType.Log;
                case TkLogLevel.Warning:
                    return LogType.Warning;
                case TkLogLevel.Error:
                    return LogType.Error;
                case TkLogLevel.Exception:
                    return LogType.Exception;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static TkLogLevel TypeToLevel(LogType logType)
        {
            switch (logType) {
                case LogType.Error:
                    return TkLogLevel.Error;
                case LogType.Assert:
                    return TkLogLevel.Error;
                case LogType.Warning:
                    return TkLogLevel.Warning;
                case LogType.Log:
                    return TkLogLevel.Info;
                case LogType.Exception:
                    return TkLogLevel.Exception;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
            }
        }
    }
}