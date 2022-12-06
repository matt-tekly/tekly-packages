using System;

namespace Tekly.Common.TimeProviders
{
    public class LocalTimeProvider : ITimeProvider
    {
        public TkDateTime Now => TkDateTime.Now + DebugOffset;
        
        public TimeSpan DebugOffset { get; set; }
    }
}