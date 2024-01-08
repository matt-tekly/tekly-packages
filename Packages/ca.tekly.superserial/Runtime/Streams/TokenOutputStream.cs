using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

namespace Tekly.SuperSerial.Streams
{
	public class TokenOutputStream : IDisposable
	{
		private const int VERSION = 1;
		private const int FLAGS = 1;
		private const int DEFAULT_BUFFER_SIZE = 4096;
		private const int BABY_BUFFER_SIZE = 32;

		private readonly Stream m_output;
		
		private int m_bufferPosition;
		private readonly int m_bufferLimit;

		// This is just a lazy 'hack' to safely write small value types.
		// Roughly, a better solution is to copy the code from WriteRawBytes into every write function
		private readonly byte[] m_babyBuffer;
		
		// Data is written to this buffer and then flushed to the output stream
		private readonly byte[] m_buffer;
		
		public TokenOutputStream(Stream output, int bufferSize = DEFAULT_BUFFER_SIZE)
		{
			m_output = output;
			
            m_buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            m_babyBuffer = ArrayPool<byte>.Shared.Rent(BABY_BUFFER_SIZE);

			m_bufferLimit = bufferSize;

			// Reserving the first 8 bytes for a version and flags
			Write(VERSION);
			Write(FLAGS);
		}

		public void WriteIndex(int value)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, value);
			WriteRawBytes(m_babyBuffer, 0, sizeof(int));
		}
		
		public void Write(short value)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, value);
			WriteRawBytes(m_babyBuffer, 0, sizeof(short));
		}

		public void Write(int value)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, value);
			WriteRawBytes(m_babyBuffer, 0, sizeof(int));
		}

		public void Write(int value, uint id)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, id);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint), value);

			WriteRawBytes(m_babyBuffer, 0, sizeof(uint) + sizeof(int));
		}
		
		public void Write(uint value)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, value);
			WriteRawBytes(m_babyBuffer, 0, sizeof(int));
		}

		public void Write(uint value, uint id)
		{
			if (value == 0) {
				return;
			}

			SuperBitConverter.Write(m_babyBuffer, 0, id);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint), value);

			WriteRawBytes(m_babyBuffer, 0, sizeof(uint) + sizeof(uint));
		}

		public void Write(float value)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, value);
			WriteRawBytes(m_babyBuffer, 0, sizeof(float));
		}

		public void Write(float value, uint id)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, id);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint), value);
			WriteRawBytes(m_babyBuffer, 0, sizeof(uint) + sizeof(float));
		}
		
		public void Write(Vector2 value)
		{
			SuperBitConverter.Write(m_babyBuffer, sizeof(float) * 0, value.x);
			SuperBitConverter.Write(m_babyBuffer, sizeof(float) * 1, value.y);
			WriteRawBytes(m_babyBuffer, 0, sizeof(float) * 2);
		}

		public void Write(Vector2 value, uint id)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, id);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint) + sizeof(float) * 0, value.x);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint) + sizeof(float) * 1, value.y);
			WriteRawBytes(m_babyBuffer, 0, sizeof(uint) + sizeof(float) * 2);
		}
		
		public void Write(Vector3 value)
		{
			SuperBitConverter.Write(m_babyBuffer, sizeof(float) * 0, value.x);
			SuperBitConverter.Write(m_babyBuffer, sizeof(float) * 1, value.y);
			SuperBitConverter.Write(m_babyBuffer, sizeof(float) * 2, value.z);
			WriteRawBytes(m_babyBuffer, 0, sizeof(float) * 3);
		}

		public void Write(Vector3 value, uint id)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, id);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint) + sizeof(float) * 0, value.x);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint) + sizeof(float) * 1, value.y);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint) + sizeof(float) * 2, value.z);
			WriteRawBytes(m_babyBuffer, 0, sizeof(uint) + sizeof(float) * 3);
		}
		
		public void Write(Quaternion value)
		{
			SuperBitConverter.Write(m_babyBuffer, sizeof(float) * 0, value.x);
			SuperBitConverter.Write(m_babyBuffer, sizeof(float) * 1, value.y);
			SuperBitConverter.Write(m_babyBuffer, sizeof(float) * 2, value.z);
			SuperBitConverter.Write(m_babyBuffer, sizeof(float) * 3, value.w);
			WriteRawBytes(m_babyBuffer, 0, sizeof(float) * 4);
		}

		public void Write(Quaternion value, uint id)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, id);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint) + sizeof(float) * 0, value.x);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint) + sizeof(float) * 1, value.y);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint) + sizeof(float) * 2, value.z);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint) + sizeof(float) * 3, value.w);
			WriteRawBytes(m_babyBuffer, 0, sizeof(uint) + sizeof(float) * 4);
		}

		public void Write(double value)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, value);
			WriteRawBytes(m_babyBuffer, 0, sizeof(double));
		}

		public void Write(double value, uint id)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, id);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint), value);
			WriteRawBytes(m_babyBuffer, 0, sizeof(uint) + sizeof(double));
		}

		public void Write(long value)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, value);
			WriteRawBytes(m_babyBuffer, 0, sizeof(long));
		}

		public void Write(long value, uint id)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, id);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint), value);
			WriteRawBytes(m_babyBuffer, 0, sizeof(uint) + sizeof(long));
		}
		
		public void Write(DateTime dateTime)
		{
			Write(dateTime.ToBinary());
		}
		
		public void Write(DateTime dateTime, uint id)
		{
			Write(dateTime.ToBinary(), id);
		}

		public void Write(bool value)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, value);
			WriteRawBytes(m_babyBuffer, 0, 1);
		}

		public void Write(bool value, uint id)
		{
			SuperBitConverter.Write(m_babyBuffer, 0, id);
			SuperBitConverter.Write(m_babyBuffer, sizeof(uint), value);
			WriteRawBytes(m_babyBuffer, 0, sizeof(uint) + sizeof(byte));
		}

		public void Write(string value)
		{
			var length = Encoding.UTF8.GetByteCount(value);
			Write(length);

			var bytesRemaining = m_bufferLimit - m_bufferPosition;
			if (bytesRemaining >= length) {
				if (length == value.Length) {
					// The string didn't contain any characters larger than a byte so we can just write the string
					// without worrying about encoding the multibyte characters.
					for (var i = 0; i < length; i++) {
						m_buffer[m_bufferPosition + i] = (byte) value[i];
					}
				} else {
					SuperBitConverter.Write(m_buffer, m_bufferPosition, value);
				}

				m_bufferPosition += length;
			} else {
				var bytes = Encoding.UTF8.GetBytes(value);
				WriteRawBytes(bytes);
			}
		}

		/// <summary>
		/// Writes a string if it is not null and not empty.
		/// </summary>
		public void Write(string value, uint id)
		{
			if (string.IsNullOrEmpty(value)) {
				return;
			}

			Write(id);
			Write(value);
		}

		public void Dispose()
		{
			Flush();
			
			ArrayPool<byte>.Shared.Return(m_buffer);
			ArrayPool<byte>.Shared.Return(m_babyBuffer);
		}
		
		public void WriteBlittable<T>(T[] data, int count) where T : struct
		{
			Assert.IsTrue(UnsafeUtility.IsBlittable<T>());
			Write(UnsafeUtility.SizeOf<T>());
			Flush();
			
			var dataSpan = new Span<T>(data, 0, count);
			var bytes = MemoryMarshal.AsBytes(dataSpan);
			m_output.Write(bytes);
		}
		
		public void WriteBytes(byte[] bytes)
		{
			Flush();
			m_output.Write(bytes);
		}

		private void WriteRawBytes(byte[] value)
		{
			WriteRawBytes(value, 0, value.Length);
		}

		private void WriteRawBytes(byte[] value, int offset, int length)
		{
			if (m_bufferLimit - m_bufferPosition >= length) {
				// We have room in the current buffer.
				ByteArray.Copy(value, offset, m_buffer, m_bufferPosition, length);
				m_bufferPosition += length;
			} else {
				// This write extends past current buffer. Fill the rest of this buffer and flush.
				var bytesWritten = m_bufferLimit - m_bufferPosition;
				ByteArray.Copy(value, offset, m_buffer, m_bufferPosition, bytesWritten);
				offset += bytesWritten;
				length -= bytesWritten;
				m_bufferPosition = m_bufferLimit;

				Flush();
				
				if (length <= m_bufferLimit) {
					// The new data fit into the buffer
					ByteArray.Copy(value, offset, m_buffer, 0, length);
					m_bufferPosition = length;
				} else {
					// The new data doesn't fit in the buffer so we'll just write directly to the stream
					m_output.Write(value, offset, length);
				}
			}
		}

		public void Flush()
		{
			m_output.Write(m_buffer, 0, m_bufferPosition);
			m_bufferPosition = 0;
		}
	}
}