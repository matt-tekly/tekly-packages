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
        public TkLogLevel MinLogLevel { get; private set; }

        public TkLogger(Type type, TkLogLevel minLogLevel)
        {
            Type = type;
            Name = Type.Name;
            FullName = Type.FullName;
            MinLogLevel = minLogLevel;
        }

        public void OverrideMinLogLevel(TkLogLevel level)
        {
            MinLogLevel = level;
        }

#if TKLOG_DISABLE_TRACE
        [Conditional("TK_UNDEFINED")]
#endif
        public void Trace(string message)
        {
            LogMessage(TkLogLevel.Trace, message);
        }

#if TKLOG_DISABLE_TRACE
        [Conditional("TK_UNDEFINED")]
#endif
        public void Trace(string message, params (string, object)[] logParams)
        {
            LogMessage(TkLogLevel.Trace, message, logParams);
        }

#if TKLOG_DISABLE_TRACE
        [Conditional("TK_UNDEFINED")]
#endif
        public void TraceContext(string message, Object context)
        {
            LogMessage(TkLogLevel.Trace, message, context);
        }

#if TKLOG_DISABLE_TRACE
        [Conditional("TK_UNDEFINED")]
#endif
        public void TraceContext(string message, Object context, params (string, object)[] logParams)
        {
            LogMessage(TkLogLevel.Trace, message, context, logParams);
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
            LogMessage(TkLogLevel.Exception, message, (TkLoggerConstants.EXCEPTION_MESSAGE_KEY, exMessage), (TkLoggerConstants.EXCEPTION_STACKTRACE_KEY, stackTrace));
        }

#if TKLOG_DISABLE_EXCEPTION
        [Conditional("TK_UNDEFINED")]
#endif
        public void Exception(Exception exception, string message, params (string, object)[] logParams)
        {
            if (TkLogLevel.Exception < MinLogLevel) {
                return;
            }
            
            var newParams = new object[logParams.Length + 4];
            
            for (var index = 0; index < logParams.Length; index++) {
                newParams[index] = logParams[index];
            }

            newParams[logParams.Length + 1] = TkLoggerConstants.EXCEPTION_MESSAGE_KEY;
            newParams[logParams.Length + 2] = exception.Message;
            
            newParams[logParams.Length + 3] = TkLoggerConstants.EXCEPTION_STACKTRACE_KEY;
            newParams[logParams.Length + 4] = StackTraceUtility.ExtractStringFromException(exception);
            
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
            LogMessage(TkLogLevel.Exception, message, context, (TkLoggerConstants.EXCEPTION_MESSAGE_KEY, exMessage), (TkLoggerConstants.EXCEPTION_STACKTRACE_KEY, stackTrace));
        }

#if TKLOG_DISABLE_EXCEPTION
        [Conditional("TK_UNDEFINED")]
#endif
        public void ExceptionContext(Exception exception, string message, Object context, params (string, object)[] logParams)
        {
            if (TkLogLevel.Exception < MinLogLevel) {
                return;
            }
            
            var newParams = new object[logParams.Length + 4];
            
            for (var index = 0; index < logParams.Length; index++) {
                newParams[index] = logParams[index];
            }

            newParams[logParams.Length + 1] = TkLoggerConstants.EXCEPTION_MESSAGE_KEY;
            newParams[logParams.Length + 2] = exception.Message;
            
            newParams[logParams.Length + 3] = TkLoggerConstants.EXCEPTION_STACKTRACE_KEY;
            newParams[logParams.Length + 4] = StackTraceUtility.ExtractStringFromException(exception);
            
            LogMessage(TkLogLevel.Exception, message, context, logParams);
        }

        private void LogMessage(TkLogLevel level, string message)
        {
            if (level < MinLogLevel) {
                return;
            }
            
            LogToDestinations(new TkLogMessage(level, Name, FullName, message, GetStackTrace()));
        }

        private void LogMessage(TkLogLevel level, string message, params (string, object)[] logParams)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestinations(new TkLogMessage(level, Name, FullName, message, GetStackTrace(), logParams));
        }

        private void LogMessage(TkLogLevel level, string message, Object context)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestinations(new TkLogMessage(level, Name, FullName, message, GetStackTrace()), context);
        }

        private void LogMessage(TkLogLevel level, string message, Object context, params (string, object)[] logParams)
        {
            if (level < MinLogLevel) {
                return;
            }

            LogToDestinations(new TkLogMessage(level, Name, FullName, message, GetStackTrace(), logParams), context);
        }
    }
}