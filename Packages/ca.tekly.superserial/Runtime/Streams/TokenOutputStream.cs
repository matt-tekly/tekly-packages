using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Tekly.SuperSerial.Streams
{
	/// <summary>
	/// Buffered little endian writer for the SuperSerial format.
	///
	/// The plain Write methods emit untagged values and are what ISuperSerialize implementations use.
	/// The WriteField methods emit [hash][WireType][payload] so that a reader can skip fields it doesn't know.
	/// </summary>
	public class TokenOutputStream : IDisposable
	{
		/// <summary>
		/// The format version this build writes. Bump it when the layout changes, and keep reading the old
		/// layout by branching on TokenInputStream.Version until MIN_VERSION moves past it.
		/// </summary>
		public const byte VERSION = 2;

		/// <summary>
		/// The oldest format version this build can still read.
		/// </summary>
		public const byte MIN_VERSION = 2;

		public const int DEFAULT_BUFFER_SIZE = 4096;

		internal static readonly byte[] MAGIC = { (byte) 'T', (byte) 'K', (byte) 'S', (byte) 'S' };
		public const int HEADER_SIZE = 6;

		private readonly Stream m_output;
		private readonly bool m_leaveOpen;
		private readonly List<int> m_blockStarts = new List<int>();

		private byte[] m_buffer;
		private int m_position;
		private bool m_disposed;

		public TokenOutputStream(Stream output, int bufferSize = DEFAULT_BUFFER_SIZE, bool leaveOpen = false)
		{
			m_output = output ?? throw new ArgumentNullException(nameof(output));
			m_leaveOpen = leaveOpen;
			m_buffer = ArrayPool<byte>.Shared.Rent(Math.Max(bufferSize, 64));

			WriteBytes(MAGIC);
			WriteByte(VERSION);
			WriteByte(0); // Flags, reserved
		}

		public void WriteByte(byte value)
		{
			Reserve(1)[0] = value;
		}

		public void Write(bool value)
		{
			WriteByte(value ? (byte) 1 : (byte) 0);
		}

		public void Write(short value)
		{
			BinaryPrimitives.WriteInt16LittleEndian(Reserve(sizeof(short)), value);
		}

		public void Write(ushort value)
		{
			BinaryPrimitives.WriteUInt16LittleEndian(Reserve(sizeof(ushort)), value);
		}

		public void Write(int value)
		{
			BinaryPrimitives.WriteInt32LittleEndian(Reserve(sizeof(int)), value);
		}

		public void Write(uint value)
		{
			BinaryPrimitives.WriteUInt32LittleEndian(Reserve(sizeof(uint)), value);
		}

		public void Write(long value)
		{
			BinaryPrimitives.WriteInt64LittleEndian(Reserve(sizeof(long)), value);
		}

		public void Write(ulong value)
		{
			BinaryPrimitives.WriteUInt64LittleEndian(Reserve(sizeof(ulong)), value);
		}

		public void Write(float value)
		{
			BinaryPrimitives.WriteInt32LittleEndian(Reserve(sizeof(float)), BitConverter.SingleToInt32Bits(value));
		}

		public void Write(double value)
		{
			BinaryPrimitives.WriteInt64LittleEndian(Reserve(sizeof(double)), BitConverter.DoubleToInt64Bits(value));
		}

		public void Write(DateTime value)
		{
			Write(value.ToBinary());
		}

		public void Write(Vector2 value)
		{
			Write(value.x);
			Write(value.y);
		}

		public void Write(Vector3 value)
		{
			Write(value.x);
			Write(value.y);
			Write(value.z);
		}

		public void Write(Vector4 value)
		{
			Write(value.x);
			Write(value.y);
			Write(value.z);
			Write(value.w);
		}

		public void Write(Quaternion value)
		{
			Write(value.x);
			Write(value.y);
			Write(value.z);
			Write(value.w);
		}

		/// <summary>
		/// Writes a string that may be null. Encoded as [byteCount + 1 : varint][utf8], with 0 meaning null.
		/// </summary>
		public void Write(string value)
		{
			if (value == null) {
				WriteVarUInt(0);
				return;
			}

			var byteCount = Encoding.UTF8.GetByteCount(value);
			WriteVarUInt((ulong) byteCount + 1);
			WriteUtf8Chars(value, byteCount);
		}

		/// <summary>
		/// Writes a non null string as [byteCount : varint][utf8]. This is the payload of WireType.Bytes.
		/// </summary>
		public void WriteUtf8(string value)
		{
			var byteCount = Encoding.UTF8.GetByteCount(value);
			WriteVarUInt((ulong) byteCount);
			WriteUtf8Chars(value, byteCount);
		}

		public void WriteVarUInt(ulong value)
		{
			while (value >= 0x80) {
				WriteByte((byte) (value | 0x80));
				value >>= 7;
			}

			WriteByte((byte) value);
		}

		public void WriteVarInt(long value)
		{
			WriteVarUInt((ulong) ((value << 1) ^ (value >> 63)));
		}

		/// <summary>
		/// Writes bytes with no length prefix.
		/// </summary>
		public void WriteBytes(ReadOnlySpan<byte> bytes)
		{
			if (bytes.Length <= m_buffer.Length - m_position) {
				bytes.CopyTo(new Span<byte>(m_buffer, m_position, bytes.Length));
				m_position += bytes.Length;
				return;
			}

			if (m_blockStarts.Count > 0) {
				Grow(m_position + bytes.Length);
				bytes.CopyTo(new Span<byte>(m_buffer, m_position, bytes.Length));
				m_position += bytes.Length;
				return;
			}

			Flush();

			if (bytes.Length >= m_buffer.Length) {
				m_output.Write(bytes);
			} else {
				bytes.CopyTo(m_buffer);
				m_position = bytes.Length;
			}
		}

		/// <summary>
		/// Writes [length : varint][bytes]. This is the payload of WireType.Bytes.
		/// </summary>
		public void WriteBytesWithLength(ReadOnlySpan<byte> bytes)
		{
			WriteVarUInt((ulong) bytes.Length);
			WriteBytes(bytes);
		}


		public void WriteWireType(WireType wireType)
		{
			WriteByte((byte) wireType);
		}

		public void WriteFieldHash(uint hash)
		{
			Write(hash);
		}

		/// <summary>
		/// Terminates an object's field list.
		/// </summary>
		public void WriteEndObject()
		{
			Write(0u);
		}

		public void WriteNullField(uint hash)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Null);
		}

		public void WriteField(uint hash, bool value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Varint);
			WriteVarUInt(value ? 1u : 0u);
		}

		public void WriteField(uint hash, sbyte value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Varint);
			WriteVarInt(value);
		}

		public void WriteField(uint hash, byte value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Varint);
			WriteVarUInt(value);
		}

		public void WriteField(uint hash, short value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Varint);
			WriteVarInt(value);
		}

		public void WriteField(uint hash, ushort value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Varint);
			WriteVarUInt(value);
		}

		public void WriteField(uint hash, char value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Varint);
			WriteVarUInt(value);
		}

		public void WriteField(uint hash, int value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Varint);
			WriteVarInt(value);
		}

		public void WriteField(uint hash, uint value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Varint);
			WriteVarUInt(value);
		}

		public void WriteField(uint hash, long value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Varint);
			WriteVarInt(value);
		}

		public void WriteField(uint hash, ulong value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Varint);
			WriteVarUInt(value);
		}

		public void WriteField(uint hash, float value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Fixed32);
			Write(value);
		}

		public void WriteField(uint hash, double value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Fixed64);
			Write(value);
		}

		public void WriteField(uint hash, DateTime value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Fixed64);
			Write(value);
		}

		public void WriteField(uint hash, string value)
		{
			WriteFieldHash(hash);

			if (value == null) {
				WriteWireType(WireType.Null);
				return;
			}

			WriteWireType(WireType.Bytes);
			WriteUtf8(value);
		}

		public void WriteField(uint hash, Vector2 value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Bytes);
			WriteVarUInt(sizeof(float) * 2);
			Write(value);
		}

		public void WriteField(uint hash, Vector3 value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Bytes);
			WriteVarUInt(sizeof(float) * 3);
			Write(value);
		}

		public void WriteField(uint hash, Quaternion value)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Bytes);
			WriteVarUInt(sizeof(float) * 4);
			Write(value);
		}

		/// <summary>
		/// Starts a field whose value is a Block: [hash][WireType.Block][length]. Write the value, then EndBlock.
		/// </summary>
		public void BeginBlockField(uint hash)
		{
			WriteFieldHash(hash);
			WriteWireType(WireType.Block);
			BeginBlock();
		}

		/// <summary>
		/// Starts a length prefixed block. Everything written until the matching EndBlock stays in memory so the
		/// length can be patched in, so keep blocks reasonably small.
		/// </summary>
		public void BeginBlock()
		{
			m_blockStarts.Add(m_position);
			Write(0);
		}

		public void EndBlock()
		{
			if (m_blockStarts.Count == 0) {
				throw new SuperSerialException("EndBlock called without a matching BeginBlock");
			}

			var start = m_blockStarts[m_blockStarts.Count - 1];
			m_blockStarts.RemoveAt(m_blockStarts.Count - 1);

			var length = m_position - start - sizeof(int);
			BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(m_buffer, start, sizeof(int)), length);

			if (m_blockStarts.Count == 0 && m_position >= DEFAULT_BUFFER_SIZE) {
				Flush();
			}
		}


		/// <summary>
		/// Writes count elements of a blittable array as raw memory, preceded by the count, the stride and a
		/// description of the struct's field layout so the reader can migrate if the struct has changed.
		/// </summary>
		public void WriteBlittable<T>(T[] data, int count) where T : struct
		{
			if (!UnsafeUtility.IsBlittable<T>()) {
				throw new SuperSerialException($"{typeof(T)} is not blittable");
			}

			if (count < 0 || count > data.Length) {
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			var layout = BlittableLayout.Get(typeof(T));

			WriteVarUInt((ulong) count);
			WriteVarUInt((ulong) layout.Stride);
			layout.Write(this);

			WriteBytes(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(data, 0, count)));
		}

		public void Flush()
		{
			if (m_blockStarts.Count > 0) {
				// Can't flush while a block is open. The data goes out when the outermost block ends.
				return;
			}

			if (m_position > 0) {
				m_output.Write(m_buffer, 0, m_position);
				m_position = 0;
			}
		}

		public void Dispose()
		{
			if (m_disposed) {
				return;
			}

			m_disposed = true;

			if (m_blockStarts.Count > 0) {
				// Don't throw from Dispose. Whatever is in the buffer is written so the failure is at least visible.
				Debug.LogError("[SuperSerial] TokenOutputStream disposed with an open block. The output is incomplete.");
				m_blockStarts.Clear();
			}

			Flush();
			m_output.Flush();

			ArrayPool<byte>.Shared.Return(m_buffer);
			m_buffer = null;

			if (!m_leaveOpen) {
				m_output.Dispose();
			}
		}

		private void WriteUtf8Chars(string value, int byteCount)
		{
			var span = Reserve(byteCount);

			if (byteCount == value.Length) {
				// Pure ASCII. Skip the encoder.
				for (var i = 0; i < byteCount; i++) {
					span[i] = (byte) value[i];
				}
			} else {
				Encoding.UTF8.GetBytes(value.AsSpan(), span);
			}
		}

		/// <summary>
		/// Returns a span of count bytes in the buffer and advances past it.
		/// </summary>
		private Span<byte> Reserve(int count)
		{
			if (m_buffer.Length - m_position < count) {
				MakeRoom(count);
			}

			var span = new Span<byte>(m_buffer, m_position, count);
			m_position += count;
			return span;
		}

		private void MakeRoom(int count)
		{
			if (m_blockStarts.Count == 0) {
				Flush();
				if (m_buffer.Length >= count) {
					return;
				}
			}

			Grow(m_position + count);
		}

		private void Grow(int minimumSize)
		{
			var newBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(minimumSize, m_buffer.Length * 2));
			Buffer.BlockCopy(m_buffer, 0, newBuffer, 0, m_position);
			ArrayPool<byte>.Shared.Return(m_buffer);
			m_buffer = newBuffer;
		}
	}
}
