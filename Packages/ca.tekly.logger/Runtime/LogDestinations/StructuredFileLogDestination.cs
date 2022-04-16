using System.Text;
using UnityEngine.Scripting;

namespace Tekly.Logging.LogDestinations
{
    [Preserve]
    public class StructuredFileLogConfig : FileLogConfig
    {
        public override ILogDestination CreateInstance()
        {
            return new StructuredFileLogDestination(this);
        }
    }
    
    /// <summary>
    /// Writes structured log files where each log message is a JSON blob separated by a new line character.
    /// This format is good for ingesting the log into a log viewer.
    /// </summary>
    public class StructuredFileLogDestination : FileLogDestination
    {
        public StructuredFileLogDestination(StructuredFileLogConfig config) : base(config) { }

        protected override void ConvertLogMessage(TkLogMessage logMessage, StringBuilder sb)
        {
            logMessage.ToJson(sb);
            
            sb.Replace("\\", "/");
            sb.Replace("\n", "\\n");
            sb.AppendLine();
        }
    }
}