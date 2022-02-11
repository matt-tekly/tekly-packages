namespace Tekly.Common.Utils
{
    public class ObjectPool<T> : ObjectPoolBase<T> where T : class, new()
    {
        protected override T Allocate()
        {
            return new T();
        }

        protected override void Recycled(T element)
        {
            
        }
    }
}