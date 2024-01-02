using System;
using System.Buffers;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

namespace Tekly.SuperSerial.Streams
{
	/// <summary>
	/// TODO: The reads could be optimized by better utilizing the buffer:
	///       1. Only directly read from the buffer - have to store a buffer position and length now
	///       2. Always attempt to read BufferSize amounts from the stream.
	///		  3. Check if the right amount of bytes are read in each Read* function
	/// </summary>
	public class TokenInputStream : IDisposable
	{
		private const int BufferSize = 4096;

		private readonly Stream m_input;
		private readonly bool m_leaveOpen;
		private readonly byte[] m_buffer;

		private readonly int m_version;
		private readonly int m_flags;

		private readonly ArrayPool<byte> m_pool;

		public TokenInputStream(Stream input, bool leaveOpen = false)
		{
			Assert.IsNotNull(input, "Input Stream is null");

			m_input = input;
			m_leaveOpen = leaveOpen;

			m_pool = ArrayPool<byte>.Shared;
			m_buffer = m_pool.Rent(BufferSize);

			m_version = ReadInt();
			m_flags = ReadInt();
		}

		public void Dispose()
		{
			if (m_input != null && !m_leaveOpen) {
				m_input.Dispose();
			}

			m_pool.Return(m_buffer);
		}

		public short ReadShort()
		{
			m_input.Read(m_buffer, 0, sizeof(short));
			return SuperBitConverter.ReadShort(m_buffer, 0);
		}
		
		public int ReadInt()
		{
			m_input.Read(m_buffer, 0, sizeof(int));
			return SuperBitConverter.ReadInt(m_buffer, 0);
		}
		
		public uint ReadUInt()
		{
			m_input.Read(m_buffer, 0, sizeof(uint));
			return SuperBitConverter.ReadUInt(m_buffer, 0);
		}

		public long ReadLong()
		{
			m_input.Read(m_buffer, 0, sizeof(long));
			return SuperBitConverter.ReadLong(m_buffer, 0);
		}
		
		public DateTime ReadDate()
		{
			return DateTime.FromBinary(ReadLong());
		}

		public float ReadFloat()
		{
			m_input.Read(m_buffer, 0, sizeof(float));
			return SuperBitConverter.ReadFloat(m_buffer, 0);
		}

		public Vector2 ReadVector2()
		{
			m_input.Read(m_buffer, 0, sizeof(float) * 2);
			return new Vector2 {
				x = SuperBitConverter.ReadFloat(m_buffer, sizeof(float) * 0),
				y = SuperBitConverter.ReadFloat(m_buffer, sizeof(float) * 1),
			};
		}

		public Vector3 ReadVector3()
		{
			m_input.Read(m_buffer, 0, sizeof(float) * 3);
			return new Vector3 {
				x = SuperBitConverter.ReadFloat(m_buffer, sizeof(float) * 0),
				y = SuperBitConverter.ReadFloat(m_buffer, sizeof(float) * 1),
				z = SuperBitConverter.ReadFloat(m_buffer, sizeof(float) * 2),
			};
		}
		
		public void Read(ref Vector3 v)
		{
			m_input.Read(m_buffer, 0, sizeof(float) * 3);
			v.x = SuperBitConverter.ReadFloat(m_buffer, sizeof(float) * 0);
			v.y = SuperBitConverter.ReadFloat(m_buffer, sizeof(float) * 1);
			v.z = SuperBitConverter.ReadFloat(m_buffer, sizeof(float) * 2);
		}
		
		public Quaternion ReadQuaternion()
		{
			m_input.Read(m_buffer, 0, sizeof(float) * 4);
			return new Quaternion {
				x = SuperBitConverter.ReadFloat(m_buffer, sizeof(float) * 0),
				y = SuperBitConverter.ReadFloat(m_buffer, sizeof(float) * 1),
				z = SuperBitConverter.ReadFloat(m_buffer, sizeof(float) * 2),
				w = SuperBitConverter.ReadFloat(m_buffer, sizeof(float) * 3),
			};
		}

		public double ReadDouble()
		{
			m_input.Read(m_buffer, 0, sizeof(double));
			return SuperBitConverter.ReadDouble(m_buffer, 0);
		}

		public bool ReadBoolean()
		{
			m_input.Read(m_buffer, 0, 1);
			return SuperBitConverter.ReadBool(m_buffer, 0);
		}

		public string ReadString()
		{
			var length = ReadInt();

			if (length < m_buffer.Length) {
				m_input.Read(m_buffer, 0, length);
				return SuperBitConverter.ReadString(m_buffer, 0, length);
			}

			var stringBuffer = m_pool.Rent(length);
			var result = SuperBitConverter.ReadString(stringBuffer, 0, length);
			m_pool.Return(stringBuffer);

			return result;
		}
	}
}