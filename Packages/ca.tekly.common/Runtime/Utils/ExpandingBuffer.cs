using System;

namespace Tekly.Common.Utils
{
    public class ExpandingBuffer
    {
        public byte[] Buffer;

        public ExpandingBuffer(int size = 0)
        {
            if (size > 0) {
                Buffer = new byte[size];
            }
        }
        
        public void EnsureSize(int size)
        {
            if (Buffer == null || size >= Buffer.Length) {
                Array.Resize(ref Buffer, size);    
            }
        }
    }
}