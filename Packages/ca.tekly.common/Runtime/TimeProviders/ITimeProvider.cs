namespace Tekly.Common.TimeProviders
{
    public interface ITimeProvider
    {
        TkDateTime Now { get; }
    }
}