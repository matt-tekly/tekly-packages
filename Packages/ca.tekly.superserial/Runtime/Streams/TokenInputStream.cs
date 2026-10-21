using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Tekly.SuperSerial.Streams
{
	/// <summary>
	/// Buffered little endian reader for the SuperSerial format. Every read either returns the full requested
	/// bytes or throws EndOfStreamException, regardless of how the underlying stream chunks its data.
	/// </summary>
	public class TokenInputStream : IDisposable
	{
		public const int DEFAULT_BUFFER_SIZE = 4096;

		private readonly Stream m_input;
		private readonly bool m_leaveOpen;

		private byte[] m_buffer;
		private int m_position;
		private int m_length;

		// Number of bytes consumed from the stream before m_buffer[0]
		private long m_bufferOffset;
		private bool m_disposed;

		public byte Version { get; }
		public byte Flags { get; }

		/// <summary>
		/// Bytes consumed so far, including the header.
		/// </summary>
		public long Position => m_bufferOffset + m_position;

		public TokenInputStream(Stream input, bool leaveOpen = false, int bufferSize = DEFAULT_BUFFER_SIZE)
		{
			m_input = input ?? throw new ArgumentNullException(nameof(input));
			m_leaveOpen = leaveOpen;
			m_buffer = ArrayPool<byte>.Shared.Rent(Math.Max(bufferSize, 64));

			Span<byte> magic = stackalloc byte[TokenOutputStream.MAGIC.Length];
			ReadBytes(magic);

			if (!magic.SequenceEqual(TokenOutputStream.MAGIC)) {
				throw new SuperSerialException("Stream is not SuperSerial data");
			}

			Version = ReadByte();
			Flags = ReadByte();

			if (Version > TokenOutputStream.VERSION) {
				throw new SuperSerialException($"Stream version {Version} is newer than supported version {TokenOutputStream.VERSION}");
			}

			if (Version < TokenOutputStream.MIN_VERSION) {
				throw new SuperSerialException($"Stream version {Version} is older than the oldest supported version {TokenOutputStream.MIN_VERSION}");
			}
		}

		public byte ReadByte()
		{
			return Take(1)[0];
		}

		public bool ReadBoolean()
		{
			return ReadByte() != 0;
		}

		public short ReadShort()
		{
			return BinaryPrimitives.ReadInt16LittleEndian(Take(sizeof(short)));
		}

		public ushort ReadUShort()
		{
			return BinaryPrimitives.ReadUInt16LittleEndian(Take(sizeof(ushort)));
		}

		public int ReadInt()
		{
			return BinaryPrimitives.ReadInt32LittleEndian(Take(sizeof(int)));
		}

		public uint ReadUInt()
		{
			return BinaryPrimitives.ReadUInt32LittleEndian(Take(sizeof(uint)));
		}

		public long ReadLong()
		{
			return BinaryPrimitives.ReadInt64LittleEndian(Take(sizeof(long)));
		}

		public ulong ReadULong()
		{
			return BinaryPrimitives.ReadUInt64LittleEndian(Take(sizeof(ulong)));
		}

		public float ReadFloat()
		{
			return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(Take(sizeof(float))));
		}

		public double ReadDouble()
		{
			return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(Take(sizeof(double))));
		}

		public DateTime ReadDate()
		{
			return DateTime.FromBinary(ReadLong());
		}

		public Vector2 ReadVector2()
		{
			return new Vector2(ReadFloat(), ReadFloat());
		}

		public Vector3 ReadVector3()
		{
			return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
		}

		public void Read(ref Vector3 value)
		{
			value.x = ReadFloat();
			value.y = ReadFloat();
			value.z = ReadFloat();
		}

		public Vector4 ReadVector4()
		{
			return new Vector4(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
		}

		public Quaternion ReadQuaternion()
		{
			return new Quaternion(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
		}

		/// <summary>
		/// Reads a string written by TokenOutputStream.Write(string). May return null.
		/// </summary>
		public string ReadString()
		{
			var encoded = ReadVarUInt();
			if (encoded == 0) {
				return null;
			}

			return ReadUtf8Chars(ToLength(encoded - 1));
		}

		/// <summary>
		/// Reads the payload of WireType.Bytes as a string.
		/// </summary>
		public string ReadUtf8()
		{
			return ReadUtf8Chars(ToLength(ReadVarUInt()));
		}

		public ulong ReadVarUInt()
		{
			ulong result = 0;
			var shift = 0;

			while (true) {
				var b = ReadByte();
				result |= (ulong) (b & 0x7F) << shift;

				if ((b & 0x80) == 0) {
					return result;
				}

				shift += 7;

				if (shift > 63) {
					throw new SuperSerialException("Varint is too long");
				}
			}
		}

		public long ReadVarInt()
		{
			var value = ReadVarUInt();
			return (long) (value >> 1) ^ -(long) (value & 1);
		}

		/// <summary>
		/// Fills the destination completely or throws EndOfStreamException.
		/// </summary>
		public void ReadBytes(Span<byte> destination)
		{
			var available = m_length - m_position;

			if (available >= destination.Length) {
				new ReadOnlySpan<byte>(m_buffer, m_position, destination.Length).CopyTo(destination);
				m_position += destination.Length;
				return;
			}

			new ReadOnlySpan<byte>(m_buffer, m_position, available).CopyTo(destination);
			DropBuffer();

			var remaining = destination.Slice(available);

			while (remaining.Length > 0) {
				var read = m_input.Read(remaining);
				if (read <= 0) {
					throw new EndOfStreamException();
				}

				m_bufferOffset += read;
				remaining = remaining.Slice(read);
			}
		}

		/// <summary>
		/// Reads the payload of WireType.Bytes into a new array.
		/// </summary>
		public byte[] ReadBytesWithLength()
		{
			var length = ToLength(ReadVarUInt());
			var bytes = new byte[length];
			ReadBytes(bytes);
			return bytes;
		}

		public void SkipBytes(long count)
		{
			if (count < 0) {
				throw new SuperSerialException("Cannot skip a negative number of bytes");
			}

			var available = m_length - m_position;

			if (count <= available) {
				m_position += (int) count;
				return;
			}

			count -= available;
			DropBuffer();

			if (m_input.CanSeek) {
				var remainingInStream = m_input.Length - m_input.Position;
				if (count > remainingInStream) {
					throw new EndOfStreamException();
				}

				m_input.Seek(count, SeekOrigin.Current);
				m_bufferOffset += count;
				return;
			}

			while (count > 0) {
				var read = m_input.Read(m_buffer, 0, (int) Math.Min(count, m_buffer.Length));
				if (read <= 0) {
					throw new EndOfStreamException();
				}

				m_bufferOffset += read;
				count -= read;
			}
		}


		public WireType ReadWireType()
		{
			var value = ReadByte();
			if (value > (byte) WireType.Raw) {
				throw new SuperSerialException($"Unknown wire type {value} at position {Position - 1}");
			}

			return (WireType) value;
		}

		/// <summary>
		/// Reads the next field's hash. Returns 0 at the end of an object.
		/// </summary>
		public uint ReadFieldHash()
		{
			return ReadUInt();
		}

		/// <summary>
		/// Reads a block's length prefix. Used with Position to verify the block was consumed exactly.
		/// </summary>
		public int ReadBlockLength()
		{
			var length = ReadInt();
			if (length < 0) {
				throw new SuperSerialException("Negative block length");
			}

			return length;
		}

		/// <summary>
		/// Skips a complete value, including its wire type byte.
		/// </summary>
		public void SkipValue()
		{
			Skip(ReadWireType());
		}

		/// <summary>
		/// Skips the payload of a value whose wire type has already been read.
		/// </summary>
		public void Skip(WireType wireType)
		{
			switch (wireType) {
				case WireType.Null:
					break;
				case WireType.Varint:
					ReadVarUInt();
					break;
				case WireType.Fixed32:
					SkipBytes(4);
					break;
				case WireType.Fixed64:
					SkipBytes(8);
					break;
				case WireType.Bytes:
					SkipBytes((long) ReadVarUInt());
					break;
				case WireType.Object:
					while (ReadFieldHash() != 0) {
						SkipValue();
					}
					break;
				case WireType.TypedObject:
					SkipBytes(sizeof(uint));
					SkipValue();
					break;
				case WireType.List: {
					var count = ReadVarUInt();
					for (ulong i = 0; i < count; i++) {
						SkipValue();
					}
					break;
				}
				case WireType.Map: {
					var count = ReadVarUInt();
					for (ulong i = 0; i < count; i++) {
						SkipValue();
						SkipValue();
					}
					break;
				}
				case WireType.Block:
					SkipBytes(ReadBlockLength());
					break;
				case WireType.Raw:
					throw new SuperSerialException("Raw values have no length and cannot be skipped");
				default:
					throw new ArgumentOutOfRangeException(nameof(wireType), wireType, null);
			}
		}


		/// <summary>
		/// Reads count elements written by WriteBlittable into destination. If the struct's layout changed since the
		/// data was written, fields are matched by name and size and anything unmatched is left zeroed.
		/// </summary>
		public void ReadBlittable<T>(T[] destination, int count) where T : struct
		{
			if (!UnsafeUtility.IsBlittable<T>()) {
				throw new SuperSerialException($"{typeof(T)} is not blittable");
			}

			var storedCount = ToLength(ReadVarUInt());
			if (storedCount != count) {
				throw new SuperSerialException($"Expected {count} elements of {typeof(T)} but the stream holds {storedCount}");
			}

			if (count > destination.Length) {
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			var sourceStride = ToLength(ReadVarUInt());
			var sourceLayout = BlittableLayout.Read(this, sourceStride);
			var destinationLayout = BlittableLayout.Get(typeof(T));

			var destinationBytes = MemoryMarshal.AsBytes(new Span<T>(destination, 0, count));

			if (destinationLayout.Matches(sourceLayout)) {
				ReadBytes(destinationBytes);
				return;
			}

			Debug.LogWarning($"[SuperSerial] Layout of {typeof(T)} changed since it was written. Migrating fields by name.");

			var sourceBytes = ArrayPool<byte>.Shared.Rent(sourceStride * count);

			try {
				var source = new Span<byte>(sourceBytes, 0, sourceStride * count);
				ReadBytes(source);
				destinationBytes.Clear();
				BlittableLayout.Migrate(source, sourceLayout, destinationBytes, destinationLayout, count);
			} finally {
				ArrayPool<byte>.Shared.Return(sourceBytes);
			}
		}

		public void Dispose()
		{
			if (m_disposed) {
				return;
			}

			m_disposed = true;

			ArrayPool<byte>.Shared.Return(m_buffer);
			m_buffer = null;

			if (!m_leaveOpen) {
				m_input.Dispose();
			}
		}

		private string ReadUtf8Chars(int byteCount)
		{
			if (byteCount == 0) {
				return string.Empty;
			}

			if (byteCount <= m_buffer.Length) {
				return Encoding.UTF8.GetString(Take(byteCount));
			}

			var bytes = ArrayPool<byte>.Shared.Rent(byteCount);
			try {
				var span = new Span<byte>(bytes, 0, byteCount);
				ReadBytes(span);
				return Encoding.UTF8.GetString(span);
			} finally {
				ArrayPool<byte>.Shared.Return(bytes);
			}
		}

		/// <summary>
		/// Returns count bytes from the buffer and advances past them. count must fit in the buffer.
		/// </summary>
		private ReadOnlySpan<byte> Take(int count)
		{
			if (m_length - m_position < count) {
				Fill(count);
			}

			var span = new ReadOnlySpan<byte>(m_buffer, m_position, count);
			m_position += count;
			return span;
		}

		/// <summary>
		/// Ensures at least count unread bytes are buffered.
		/// </summary>
		private void Fill(int count)
		{
			if (m_position > 0) {
				var remaining = m_length - m_position;
				if (remaining > 0) {
					Buffer.BlockCopy(m_buffer, m_position, m_buffer, 0, remaining);
				}

				m_bufferOffset += m_position;
				m_length = remaining;
				m_position = 0;
			}

			if (m_buffer.Length < count) {
				var newBuffer = ArrayPool<byte>.Shared.Rent(count);
				Buffer.BlockCopy(m_buffer, 0, newBuffer, 0, m_length);
				ArrayPool<byte>.Shared.Return(m_buffer);
				m_buffer = newBuffer;
			}

			while (m_length < count) {
				var read = m_input.Read(m_buffer, m_length, m_buffer.Length - m_length);
				if (read <= 0) {
					throw new EndOfStreamException();
				}

				m_length += read;
			}
		}

		private void DropBuffer()
		{
			m_bufferOffset += m_length;
			m_position = 0;
			m_length = 0;
		}

		private static int ToLength(ulong value)
		{
			if (value > int.MaxValue) {
				throw new SuperSerialException($"Length {value} is too large");
			}

			return (int) value;
		}
	}
}
