using System;

namespace Tekly.Common.Utils
{
    public interface IQuantity<out T>
    {
        double GetCount();
        T GetValue();
    }

    [Serializable]
    public struct Quantity<T> : IQuantity<T>
    {
        public double Count;
        public T Value;
        
        public double GetCount() => Count;
        public T GetValue() => Value;
    }
}