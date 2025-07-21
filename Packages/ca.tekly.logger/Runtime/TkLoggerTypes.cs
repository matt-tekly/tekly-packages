using Tekly.Logging.Configurations;

namespace Tekly.Logging
{
    public enum TkLogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Exception,
        None
    }
    
    public enum LogSource
    {
        TkLogger,
        Unity
    }
    
    public enum LogPrefixes
    {
        Logger,
        Frame,
        Level,
        Timestamp,
    }
    
    public class LoggerSettings
    {
        public TkLogLevel Level;
        public ILogDestination Destination;
        
        public LoggerSettings(TkLogLevel level, ILogDestination destination)
        {
            Level = level;
            Destination = destination;
        }
        
        public LoggerSettings(TkLogLevel level, LogDestinationConfig destinationConfig)
        {
            Level = level;
            Destination = TkLogger.GetDestination(destinationConfig);
        }
    }
}