using System;
using System.Xml.Serialization;
using Object = UnityEngine.Object;

namespace Tekly.Logging
{
    public interface ILogDestination : IDisposable
    {
        string Name { get; }
        void LogMessage(TkLogMessage message, LogSource logSource);
        void LogMessage(TkLogMessage message, Object context, LogSource logSource);
        void Update();
    }
}