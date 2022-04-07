using System;

namespace Tekly.Logging
{
    public interface ITkLogDestination : IDisposable
    {
        void LogMessage(TkLogMessage message);
        void LogMessage(TkLogMessage message, UnityEngine.Object context);
        void Update();
    }
}