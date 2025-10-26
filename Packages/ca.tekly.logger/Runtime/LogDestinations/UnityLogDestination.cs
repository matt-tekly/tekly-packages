using System;
using System.Text;
using System.Threading;
using Tekly.Logging.Configurations;
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
        
        [ThreadStatic] public static bool Suppress;

        public static IDisposable SuppressScope()
        {
            var prev = Suppress;
            Suppress = true;
            return new Scope(prev);
        }

        private readonly struct Scope : IDisposable
        {
            private readonly bool _prev;
            public Scope(bool prev) { _prev = prev; }
            public void Dispose() { Suppress = _prev; }
        }
        
        private int m_currentFrame;

        private readonly UnityLogDestinationConfig m_config;
        private readonly ThreadLocal<StringBuilder> m_stringBuilders = new ThreadLocal<StringBuilder>(() => new StringBuilder(512));

#if UNITY_EDITOR
        private Regex m_linkRegex = new Regex("(.* \\(at )(([^\\/].*):([0-9]+))", RegexOptions.RightToLeft);
#endif

        public UnityLogDestination(string name, UnityLogDestinationConfig config)
        {
            m_config = config;
            Name = name;
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            if (!UnityEditor.AssetDatabase.Contains(m_config)) {
                Object.DestroyImmediate(m_config);
            }
#endif
        }

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

            for (var i = 0; i < m_config.Prefixes.Length; i++) {
                switch (m_config.Prefixes[i]) {
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
                    case LogPrefixes.Timestamp:
                        var dateTime = m_config.UseUtc ? DateTime.UtcNow : DateTime.Now;
                        sb.Append("[");
                        sb.Append(dateTime.ToString(m_config.TimeFormat));
                        sb.Append("]");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (m_config.Separator.Length > 0 && i < m_config.Prefixes.Length - 1) {
                    sb.Append(m_config.Separator);
                }
            }
            
            if (m_config.Prefixes.Length > 0 && m_config.MessageSeparator.Length > 0) {
                sb.Append(m_config.MessageSeparator);
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
            
            try {
                using var _ = SuppressScope();
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