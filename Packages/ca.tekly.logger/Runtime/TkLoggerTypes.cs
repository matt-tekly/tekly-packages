using System.Collections.Generic;

namespace Tekly.Logging
{
    public enum TkLogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Exception,
    }
    
    public enum LogSource
    {
        TkLogger,
        Unity
    }
    
    public class LoggerGroup
    {
        public string Name;
        public List<ILogDestination> Destinations = new List<ILogDestination>();
    }
    
    public class LoggerSettings
    {
        public TkLogLevel Level;
        public LoggerGroup Group;
    }
}