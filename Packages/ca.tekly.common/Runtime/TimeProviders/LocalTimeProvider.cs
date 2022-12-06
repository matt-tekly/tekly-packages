using System;

namespace Tekly.Common.TimeProviders
{
    public class LocalTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow + DebugOffset;
        public DateTime Now => DateTime.Now + DebugOffset;
        
        public TimeSpan DebugOffset { get; set; }
    }
}