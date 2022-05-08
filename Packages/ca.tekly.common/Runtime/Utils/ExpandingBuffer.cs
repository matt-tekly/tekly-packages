using System;

namespace Tekly.Common.Utils
{
    public class ExpandingBuffer<T>
    {
        public T[] Buffer;

        public ExpandingBuffer(int size = 0)
        {
            if (size > 0) {
                Buffer = new T[size];
            } else {
                Buffer = Array.Empty<T>();
            }
        }
        
        public void EnsureSize(int size)
        {
            if (size >= Buffer.Length) {
                Array.Resize(ref Buffer, size);    
            }
        }
    }
}