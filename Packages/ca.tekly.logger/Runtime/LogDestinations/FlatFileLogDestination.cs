using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Tekly.Logging.LogDestinations
{
    public class FlatFileLogConfig : FileLogConfig
    {
        public override ILogDestination CreateInstance()
        {
            return new FlatFileLogDestination(this);
        }
    }
    
    public class FlatFileLogDestination : FileLogDestination
    {
        public FlatFileLogDestination(FlatFileLogConfig config) : base(config) { }

        protected override void ConvertLogMessage(TkLogMessage message, StringBuilder sb)
        {
            var timeStamp = message.DateTime.ToLocalTime().ToString(LoggerConstants.TIME_FORMAT_LOCAL);
            sb.AppendFormat("[{0}] {1} [{2}] ", timeStamp, LevelToCharacter(message.Level), message.LoggerName);
            message.Print(sb);

            sb.Append("\n\n");

            var foundException = false;

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
                sb.AppendLine(message.StackTrace);
            }
        }

        private static string LevelToCharacter(TkLogLevel level)
        {
            switch (level) {
                case TkLogLevel.Debug:
                    return "[D]";
                case TkLogLevel.Info:
                    return "[I]";
                case TkLogLevel.Warning:
                    return "[W]";
                case TkLogLevel.Error:
                    return "[E]";
                case TkLogLevel.Exception:
                    return "[X]";
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }
}