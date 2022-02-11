using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Assertions;

namespace Tekly.Logging
{
    [Serializable]
    public struct TkLogMessage
    {
        public TkLogLevel Level;
        public string LoggerName;
        public string LoggerFullName;
        public string Message;
        public TkLogParam[] Params;
        public string Timestamp;
        public string StackTrace;

        public TkLogMessage(TkLogLevel level, string loggerName, string loggerFullName, string message, string stackTrace)
        {
            Level = level;
            LoggerName = loggerName;
            LoggerFullName = loggerFullName;
            Message = message;
            Params = null;
            Timestamp = DateTime.UtcNow.ToString(TkLoggerConstants.TIME_FORMAT);
            StackTrace = stackTrace;
            
            if (TkLogger.CommonFields.Count > 0) {
                Params = new TkLogParam[TkLogger.CommonFields.Count];
                CopyCommonFields(0);
            }
        }
        
        public TkLogMessage(TkLogLevel level, string loggerName, string loggerFullName, string message, string stackTrace, List<TkLogParam> logParams)
        {
            Level = level;
            LoggerName = loggerName;
            LoggerFullName = loggerFullName;
            Message = message;
            Params = new TkLogParam[logParams.Count + TkLogger.CommonFields.Count];
            Timestamp = DateTime.UtcNow.ToString(TkLoggerConstants.TIME_FORMAT);
            StackTrace = stackTrace;

            for (var index = 0; index < logParams.Count; index++) {
                Params[index] = logParams[index];
            }

            CopyCommonFields(logParams.Count);
        }
        
        public TkLogMessage(TkLogLevel level, string loggerName, string loggerFullName, string message, string stackTrace, TkLogParam[] logParams)
        {
            Level = level;
            LoggerName = loggerName;
            LoggerFullName = loggerFullName;
            Message = message;
            Params = new TkLogParam[logParams.Length + TkLogger.CommonFields.Count];
            Timestamp = DateTime.UtcNow.ToString(TkLoggerConstants.TIME_FORMAT);
            StackTrace = stackTrace;

            for (var index = 0; index < logParams.Length; index++) {
                Params[index] = logParams[index];
            }

            CopyCommonFields(logParams.Length);
        }
        
        public TkLogMessage(TkLogLevel level, string loggerName, string loggerFullName, string message, string stackTrace, params object[] logParams)
        {
            Level = level;
            LoggerName = loggerName;
            LoggerFullName = loggerFullName;
            Message = message;
            Params = TkLogParam.CreateReserve(TkLogger.CommonFields.Count, logParams);
            Timestamp = DateTime.UtcNow.ToString(TkLoggerConstants.TIME_FORMAT);
            StackTrace = stackTrace;
            
            CopyCommonFields(logParams.Length / 2);
        }
        
        public TkLogMessage(TkLogLevel level, string loggerName, string loggerFullName, string message, string stackTrace, params (string, object)[] logParams)
        {
            Level = level;
            LoggerName = loggerName;
            LoggerFullName = loggerFullName;
            Message = message;
            Params = TkLogParam.CreateReserve(TkLogger.CommonFields.Count, logParams);
            Timestamp = DateTime.UtcNow.ToString(TkLoggerConstants.TIME_FORMAT);
            StackTrace = stackTrace;
            
            CopyCommonFields(logParams.Length);
        }

        public void Print(StringBuilder sb)
        {
            sb.Append(Message);
            
            if (Params == null || !Message.Contains("{")) {
                return;
            }
            
            foreach (var param in Params) {
                sb.Replace($"{{{param.Id}}}", param.Value);
            }
        }

        /// <summary>
        /// Copying the common fields when the message is created so that we get the fields as they were when the
        /// log message was created.
        /// </summary>
        private void CopyCommonFields(int startIndex)
        {
            var index = 0;
            foreach (var commonField in TkLogger.CommonFields) {
                Params[startIndex + index] = new TkLogParam(commonField.Key, commonField.Value);
                index++;
            }
        }

        public void ToJson(StringBuilder sb)
        {
            sb.Append("{");
            sb.Append($"\"Level\":\"{Level}\"");
            sb.Append($",\"Logger\":\"{LoggerFullName}\"");
            sb.Append($",\"Message\":\"{Message}\"");
            sb.Append($",\"Timestamp\":\"{Timestamp}\"");
            
            if (!string.IsNullOrEmpty(StackTrace)) {
                sb.Append($",\"StackTrace\":\"{StackTrace}\"");    
            }
            
            if (Params != null) {
                foreach (var tkLogParam in Params) {
                    sb.Append($",\"{tkLogParam.Id}\":\"{tkLogParam.Value}\"");
                }
            }
            
            sb.Append("}");
        }
    }
    
    [Serializable]
    public struct TkLogParam
    {
        public string Id;
        public string Value;

        public TkLogParam(string id, string value)
        {
            Id = id;
            Value = value;
        }
        
        public static TkLogParam[] Create(params (string, object)[] logParams)
        {
            return CreateReserve(0, logParams);
        }
        
        public static TkLogParam[] CreateReserve(int reserve, object[] logParams)
        {
            Assert.IsTrue((logParams.Length % 2) == 0);
            
            var tkLogParams = new TkLogParam[reserve + (logParams.Length / 2)];
            
            for (var index = 0; index < logParams.Length; index += 2) {
                tkLogParams[index / 2] = new TkLogParam(logParams[index].ToString(), logParams[index + 1].ToString());
            }

            return tkLogParams;
        }

        public static TkLogParam[] CreateReserve(int reserve, (string, object)[] logParams)
        {
            var tkLogParams = new TkLogParam[reserve + logParams.Length];
            
            for (var index = 0; index < logParams.Length; index++) {
                tkLogParams[index] = new TkLogParam(logParams[index].Item1, logParams[index].Item2.ToString());
            }

            return tkLogParams;
        }
    }
}