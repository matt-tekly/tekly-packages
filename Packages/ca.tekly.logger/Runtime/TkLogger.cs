using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Logging
{
    public partial class TkLogger
    {
        public readonly Type Type;
        public readonly string Name;
        public readonly string FullName;
        public TkLogLevel MinLogLevel => LoggerSettings.Level;
        public LoggerSettings LoggerSettings { get; set; }

        public TkLogger(Type type, LoggerSettings loggerSettings)
        {
            Type = type;
            Name = Type.Name;
            FullName = Type.FullName;
            LoggerSettings = loggerSettings;
        }

        public bool IsLevelEnabled(TkLogLevel level)
        {
            return level >= MinLogLevel;
        }

        // *** BEGIN codegen ***

#if TKLOG_DISABLE_DEBUG
        [Conditional("TK_UNDEFINED")]
#endif
        public void Debug(string message)
        {
            LogMessage(TkLogLevel.Debug, message);
        }

#if TKLOG_DISABLE_DEBUG
        [Conditional("TK_UNDEFINED")]
#endif
        public void Debug<T>(string message, (string, T) logParam1)
        {
            LogMessage(TkLogLevel.Debug, message, logParam1);
        }

#if TKLOG_DISABLE_DEBUG
        [Conditional("TK_UNDEFINED")]
#endif
        public void Debug<T, U>(string message, (string, T) logParam1, (string, U) logParam2)
        {
            LogMessage(TkLogLevel.Debug, message, logParam1, logParam2);
        }

#if TKLOG_DISABLE_DEBUG
        [Conditional("TK_UNDEFINED")]
#endif
        public void Debug<T, U, V>(string message, (string, T) logParam1, (string, U) logParam2, (string, V) logParam3)
        {
            LogMessage(TkLogLevel.Debug, message, logParam1, logParam2, logParam3);
        }

#if TKLOG_DISABLE_DEBUG
        [Conditional("TK_UNDEFINED")]
#endif
        public void Debug(string message, params (string, object)[] logParams)
        {
            LogMessage(TkLogLevel.Debug, message, logParams);
        }

#if TKLOG_DISABLE_DEBUG
        [Conditional("TK_UNDEFINED")]
#endif
        public void DebugContext(string message, Object context)
        {
            LogMessage(TkLogLevel.Debug, message, context);
        }

#if TKLOG_DISABLE_DEBUG
        [Conditional("TK_UNDEFINED")]
#endif
        public void DebugContext<T>(string message, Object context, (string, T) logParam1)
        {
            LogMessage(TkLogLevel.Debug, message, context, logParam1);
        }

#if TKLOG_DISABLE_DEBUG
        [Conditional("TK_UNDEFINED")]
#endif
        public void DebugContext<T, U>(string message, Object context, (string, T) logParam1, (string, U) logParam2)
        {
            LogMessage(TkLogLevel.Debug, message, context, logParam1, logParam2);
        }

#if TKLOG_DISABLE_DEBUG
        [Conditional("TK_UNDEFINED")]
#endif
        public void DebugContext<T, U, V>(string message, Object context, (string, T) logParam1, (string, U) logParam2, (string, V) logParam3)
        {
            LogMessage(TkLogLevel.Debug, message, context, logParam1, logParam2, logParam3);
        }

#if TKLOG_DISABLE_DEBUG
        [Conditional("TK_UNDEFINED")]
#endif
        public void DebugContext(string message, Object context, params (string, object)[] logParams)
        {
            LogMessage(TkLogLevel.Debug, message, context, logParams);
        }
#if TKLOG_DISABLE_INFO
        [Conditional("TK_UNDEFINED")]
#endif
        public void Info(string message)
        {
            LogMessage(TkLogLevel.Info, message);
        }

#if TKLOG_DISABLE_INFO
        [Conditional("TK_UNDEFINED")]
#endif
        public void Info<T>(string message, (string, T) logParam1)
        {
            LogMessage(TkLogLevel.Info, message, logParam1);
        }

#if TKLOG_DISABLE_INFO
        [Conditional("TK_UNDEFINED")]
#endif
        public void Info<T, U>(string message, (string, T) logParam1, (string, U) logParam2)
        {
            LogMessage(TkLogLevel.Info, message, logParam1, logParam2);
        }

#if TKLOG_DISABLE_INFO
        [Conditional("TK_UNDEFINED")]
#endif
        public void Info<T, U, V>(string message, (string, T) logParam1, (string, U) logParam2, (string, V) logParam3)
        {
            LogMessage(TkLogLevel.Info, message, logParam1, logParam2, logParam3);
        }

#if TKLOG_DISABLE_INFO
        [Conditional("TK_UNDEFINED")]
#endif
        public void Info(string message, params (string, object)[] logParams)
        {
            LogMessage(TkLogLevel.Info, message, logParams);
        }

#if TKLOG_DISABLE_INFO
        [Conditional("TK_UNDEFINED")]
#endif
        public void InfoContext(string message, Object context)
        {
            LogMessage(TkLogLevel.Info, message, context);
        }

#if TKLOG_DISABLE_INFO
        [Conditional("TK_UNDEFINED")]
#endif
        public void InfoContext<T>(string message, Object context, (string, T) logParam1)
        {
            LogMessage(TkLogLevel.Info, message, context, logParam1);
        }

#if TKLOG_DISABLE_INFO
        [Conditional("TK_UNDEFINED")]
#endif
        public void InfoContext<T, U>(string message, Object context, (string, T) logParam1, (string, U) logParam2)
        {
            LogMessage(TkLogLevel.Info, message, context, logParam1, logParam2);
        }

#if TKLOG_DISABLE_INFO
        [Conditional("TK_UNDEFINED")]
#endif
        public void InfoContext<T, U, V>(string message, Object context, (string, T) logParam1, (string, U) logParam2, (string, V) logParam3)
        {
            LogMessage(TkLogLevel.Info, message, context, logParam1, logParam2, logParam3);
        }

#if TKLOG_DISABLE_INFO
        [Conditional("TK_UNDEFINED")]
#endif
        public void InfoContext(string message, Object context, params (string, object)[] logParams)
        {
            LogMessage(TkLogLevel.Info, message, context, logParams);
        }
#if TKLOG_DISABLE_WARNING
        [Conditional("TK_UNDEFINED")]
#endif
        public void Warning(string message)
        {
            LogMessage(TkLogLevel.Warning, message);
        }

#if TKLOG_DISABLE_WARNING
        [Conditional("TK_UNDEFINED")]
#endif
        public void Warning<T>(string message, (string, T) logParam1)
        {
            LogMessage(TkLogLevel.Warning, message, logParam1);
        }

#if TKLOG_DISABLE_WARNING
        [Conditional("TK_UNDEFINED")]
#endif
        public void Warning<T, U>(string message, (string, T) logParam1, (string, U) logParam2)
        {
            LogMessage(TkLogLevel.Warning, message, logParam1, logParam2);
        }

#if TKLOG_DISABLE_WARNING
        [Conditional("TK_UNDEFINED")]
#endif
        public void Warning<T, U, V>(string message, (string, T) logParam1, (string, U) logParam2, (string, V) logParam3)
        {
            LogMessage(TkLogLevel.Warning, message, logParam1, logParam2, logParam3);
        }

#if TKLOG_DISABLE_WARNING
        [Conditional("TK_UNDEFINED")]
#endif
        public void Warning(string message, params (string, object)[] logParams)
        {
            LogMessage(TkLogLevel.Warning, message, logParams);
        }

#if TKLOG_DISABLE_WARNING
        [Conditional("TK_UNDEFINED")]
#endif
        public void WarningContext(string message, Object context)
        {
            LogMessage(TkLogLevel.Warning, message, context);
        }

#if TKLOG_DISABLE_WARNING
        [Conditional("TK_UNDEFINED")]
#endif
        public void WarningContext<T>(string message, Object context, (string, T) logParam1)
        {
            LogMessage(TkLogLevel.Warning, message, context, logParam1);
        }

#if TKLOG_DISABLE_WARNING
        [Conditional("TK_UNDEFINED")]
#endif
        public void WarningContext<T, U>(string message, Object context, (string, T) logParam1, (string, U) logParam2)
        {
            LogMessage(TkLogLevel.Warning, message, context, logParam1, logParam2);
        }

#if TKLOG_DISABLE_WARNING
        [Conditional("TK_UNDEFINED")]
#endif
        public void WarningContext<T, U, V>(string message, Object context, (string, T) logParam1, (string, U) logParam2, (string, V) logParam3)
        {
            LogMessage(TkLogLevel.Warning, message, context, logParam1, logParam2, logParam3);
        }

#if TKLOG_DISABLE_WARNING
        [Conditional("TK_UNDEFINED")]
#endif
        public void WarningContext(string message, Object context, params (string, object)[] logParams)
        {
            LogMessage(TkLogLevel.Warning, message, context, logParams);
        }
#if TKLOG_DISABLE_ERROR
        [Conditional("TK_UNDEFINED")]
#endif
        public void Error(string message)
        {
            LogMessage(TkLogLevel.Error, message);
        }

#if TKLOG_DISABLE_ERROR
        [Conditional("TK_UNDEFINED")]
#endif
        public void Error<T>(string message, (string, T) logParam1)
        {
            LogMessage(TkLogLevel.Error, message, logParam1);
        }

#if TKLOG_DISABLE_ERROR
        [Conditional("TK_UNDEFINED")]
#endif
        public void Error<T, U>(string message, (string, T) logParam1, (string, U) logParam2)
        {
            LogMessage(TkLogLevel.Error, message, logParam1, logParam2);
        }

#if TKLOG_DISABLE_ERROR
        [Conditional("TK_UNDEFINED")]
#endif
        public void Error<T, U, V>(string message, (string, T) logParam1, (string, U) logParam2, (string, V) logParam3)
        {
            LogMessage(TkLogLevel.Error, message, logParam1, logParam2, logParam3);
        }

#if TKLOG_DISABLE_ERROR
        [Conditional("TK_UNDEFINED")]
#endif
        public void Error(string message, params (string, object)[] logParams)
        {
            LogMessage(TkLogLevel.Error, message, logParams);
        }

#if TKLOG_DISABLE_ERROR
        [Conditional("TK_UNDEFINED")]
#endif
        public void ErrorContext(string message, Object context)
        {
            LogMessage(TkLogLevel.Error, message, context);
        }

#if TKLOG_DISABLE_ERROR
        [Conditional("TK_UNDEFINED")]
#endif
        public void ErrorContext<T>(string message, Object context, (string, T) logParam1)
        {
            LogMessage(TkLogLevel.Error, message, context, logParam1);
        }

#if TKLOG_DISABLE_ERROR
        [Conditional("TK_UNDEFINED")]
#endif
        public void ErrorContext<T, U>(string message, Object context, (string, T) logParam1, (string, U) logParam2)
        {
            LogMessage(TkLogLevel.Error, message, context, logParam1, logParam2);
        }

#if TKLOG_DISABLE_ERROR
        [Conditional("TK_UNDEFINED")]
#endif
        public void ErrorContext<T, U, V>(string message, Object context, (string, T) logParam1, (string, U) logParam2, (string, V) logParam3)
        {
            LogMessage(TkLogLevel.Error, message, context, logParam1, logParam2, logParam3);
        }

#if TKLOG_DISABLE_ERROR
        [Conditional("TK_UNDEFINED")]
#endif
        public void ErrorContext(string message, Object context, params (string, object)[] logParams)
        {
            LogMessage(TkLogLevel.Error, message, context, logParams);
        }

        // *** END codegen ***


#if TKLOG_DISABLE_EXCEPTION
        [Conditional("TK_UNDEFINED")]
#endif
        public void Exception(Exception exception, string message, params (string, object)[] logParams)
        {
            if (TkLogLevel.Exception < MinLogLevel) {
                return;
            }

            var newParams = new (string, object)[logParams.Length + 2];

            for (var index = 0; index < logParams.Length; index++) {
                newParams[index] = logParams[index];
            }

            newParams[logParams.Length] = (LoggerConstants.EXCEPTION_MESSAGE_KEY, exception.Message);
            newParams[logParams.Length + 1] = (LoggerConstants.EXCEPTION_STACKTRACE_KEY, StackTraceUtility.ExtractStringFromException(exception));

            LogMessage(TkLogLevel.Exception, message, logParams);
        }

#if TKLOG_DISABLE_EXCEPTION
        [Conditional("TK_UNDEFINED")]
#endif
        public void ExceptionContext(Exception exception, string message, Object context)
        {
            if (TkLogLevel.Exception < MinLogLevel) {
                return;
            }

            StackTraceUtilities.ExtractStringFromExceptionInternal(exception, out var exMessage, out var stackTrace);
            LogMessage(TkLogLevel.Exception, message, context, (LoggerConstants.EXCEPTION_MESSAGE_KEY, exMessage), (LoggerConstants.EXCEPTION_STACKTRACE_KEY, stackTrace));
        }

#if TKLOG_DISABLE_EXCEPTION
        [Conditional("TK_UNDEFINED")]
#endif
        public void ExceptionContext(Exception exception, string message, Object context, params (string, object)[] logParams)
        {
            if (TkLogLevel.Exception < MinLogLevel) {
                return;
            }

            var newParams = new (string, object)[logParams.Length + 2];

            for (var index = 0; index < logParams.Length; index++) {
                newParams[index] = logParams[index];
            }

            newParams[logParams.Length] = (LoggerConstants.EXCEPTION_MESSAGE_KEY, exception.Message);
            newParams[logParams.Length + 1] = (LoggerConstants.EXCEPTION_STACKTRACE_KEY, StackTraceUtility.ExtractStringFromException(exception));

            LogMessage(TkLogLevel.Exception, message, context, logParams);
        }

        public void LogMessage(TkLogLevel level, string message)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestination(LoggerSettings.Destination, new TkLogMessage(level, Name, FullName, message, GetStackTrace()));
        }

        public void LogMessageStackTrace(TkLogLevel level, string message, string stackTrace)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestination(LoggerSettings.Destination, new TkLogMessage(level, Name, FullName, message, stackTrace));
        }

        public void LogMessage(TkLogLevel level, string message, params (string, object)[] logParams)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestination(LoggerSettings.Destination, new TkLogMessage(level, Name, FullName, message, GetStackTrace(), logParams));
        }

        public void LogMessage<T>(TkLogLevel level, string message, (string, T) logParam1)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestination(LoggerSettings.Destination, new TkLogMessage(level, Name, FullName, message, GetStackTrace(), logParam1));
        }

        public void LogMessage<T, U>(TkLogLevel level, string message, (string, T) logParam1, (string, U) logParam2)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestination(LoggerSettings.Destination, new TkLogMessage(level, Name, FullName, message, GetStackTrace(), logParam1, logParam2));
        }

        public void LogMessage<T, U, V>(TkLogLevel level, string message, (string, T) logParam1, (string, U) logParam2, (string, V) logParam3)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestination(LoggerSettings.Destination, new TkLogMessage(level, Name, FullName, message, GetStackTrace(), logParam1, logParam2, logParam3));
        }

        public void LogMessageStackTrace(TkLogLevel level, string message, string stacktrace, params (string, object)[] logParams)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestination(LoggerSettings.Destination, new TkLogMessage(level, Name, FullName, message, stacktrace, logParams));
        }

        public void LogMessage(TkLogLevel level, string message, Object context)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestination(LoggerSettings.Destination, new TkLogMessage(level, Name, FullName, message, GetStackTrace()), context);
        }

        public void LogMessage(TkLogLevel level, string message, Object context, params (string, object)[] logParams)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestination(LoggerSettings.Destination, new TkLogMessage(level, Name, FullName, message, GetStackTrace(), logParams), context);
        }
    }
}