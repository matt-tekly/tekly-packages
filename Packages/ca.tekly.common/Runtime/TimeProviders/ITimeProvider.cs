using System;

namespace Tekly.Common.TimeProviders
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
        DateTime Now { get; }
    }
}