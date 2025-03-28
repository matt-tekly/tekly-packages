using System.Text;

namespace Tekly.Logging.LogDestinations
{
    public class FlatFileLogDestination : FileLogDestination
    {
        public FlatFileLogDestination(string name, string prefix, TkLogLevel minimumLevel) : base(name, prefix, minimumLevel) { }

        protected override void ConvertLogMessage(TkLogMessage message, StringBuilder sb)
        {
            var timeStamp = message.DateTime.ToLocalTime().ToString(LoggerConstants.TIME_FORMAT_LOCAL);
            sb.AppendFormat("[{0}] {1} [{2}] ", timeStamp, TkLoggerUtils.LevelToCharacter(message.Level), message.LoggerName);
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

            if (!foundException && !string.IsNullOrEmpty(message.StackTrace)) {
                sb.Append(message.StackTrace).Append('\n');
            }
        }
    }
}