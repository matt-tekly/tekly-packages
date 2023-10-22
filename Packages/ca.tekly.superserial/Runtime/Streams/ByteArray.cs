using System;

namespace Tekly.SuperSerial.Streams
{
    internal static class ByteArray
    {
        private const int COPY_THRESHOLD = 12;

        internal static void Copy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
        {
            if (count > COPY_THRESHOLD)
            {
                Buffer.BlockCopy(src, srcOffset, dst, dstOffset, count);
            }
            else
            {
                int stop = srcOffset + count;
                for (int i = srcOffset; i < stop; i++)
                {
                    dst[dstOffset++] = src[i];
                }
            }
        }
    }
}