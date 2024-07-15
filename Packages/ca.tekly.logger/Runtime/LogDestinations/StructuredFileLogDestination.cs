using System.Text;

namespace Tekly.Logging.LogDestinations
{
    /// <summary>
    /// Writes structured log files where each log message is a JSON blob separated by a new line character.
    /// This format is good for ingesting the log into a log viewer.
    /// </summary>
    public class StructuredFileLogDestination : FileLogDestination
    {
        public StructuredFileLogDestination(string name, string prefix, TkLogLevel minimumLevel) : base(name, prefix, minimumLevel) { }

        protected override void ConvertLogMessage(TkLogMessage logMessage, StringBuilder sb)
        {
            logMessage.ToJson(sb);
            
            sb.Replace("\\", "/");
            sb.Replace("\n", "\\n");
            sb.Append('\n');
        }
    }
}