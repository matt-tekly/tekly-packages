namespace Tekly.Common.Observables
{
    public interface IValueObserver<in T>
    {
        void Changed(T value);
    }
}