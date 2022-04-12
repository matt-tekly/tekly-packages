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
        public void ErrorContext(string message, Object context, params (string, object)[] logParams)
        {
            LogMessage(TkLogLevel.Error, message, context, logParams);
        }
        
#if TKLOG_DISABLE_EXCEPTION
        [Conditional("TK_UNDEFINED")]
#endif
        public void Exception(Exception exception, string message)
        {
            if (TkLogLevel.Exception < MinLogLevel) {
                return;
            }

            StackTraceUtilities.ExtractStringFromExceptionInternal(exception, out var exMessage, out var stackTrace);
            LogMessage(TkLogLevel.Exception, message, (LoggerConstants.EXCEPTION_MESSAGE_KEY, exMessage), (LoggerConstants.EXCEPTION_STACKTRACE_KEY, stackTrace));
        }

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

        private void LogMessage(TkLogLevel level, string message)
        {
            if (level < MinLogLevel) {
                return;
            }
            
            LogToDestinations(LoggerSettings.Group, new TkLogMessage(level, Name, FullName, message, GetStackTrace()));
        }

        private void LogMessage(TkLogLevel level, string message, params (string, object)[] logParams)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestinations(LoggerSettings.Group, new TkLogMessage(level, Name, FullName, message, GetStackTrace(), logParams));
        }

        private void LogMessage(TkLogLevel level, string message, Object context)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestinations(LoggerSettings.Group, new TkLogMessage(level, Name, FullName, message, GetStackTrace()), context);
        }

        private void LogMessage(TkLogLevel level, string message, Object context, params (string, object)[] logParams)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestinations(LoggerSettings.Group, new TkLogMessage(level, Name, FullName, message, GetStackTrace(), logParams), context);
        }
    }
}