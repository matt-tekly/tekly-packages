namespace Tekly.Logging
{
    public interface ITkLogDestination
    {
        void LogMessage(TkLogMessage message);
        void LogMessage(TkLogMessage message, UnityEngine.Object context);
        void Update();
    }
}