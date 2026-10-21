using System;
using System.IO;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Tests
{
	/// <summary>
	/// Round trip helpers shared by the serializer tests.
	/// </summary>
	public static class Serial
	{
		public static MemoryStream Write(object value, SuperSerializer serializer = null)
		{
			serializer ??= new SuperSerializer();
			var memory = new MemoryStream();

			using (var output = new TokenOutputStream(memory, leaveOpen: true)) {
				serializer.Write(output, value);
			}

			memory.Position = 0;
			return memory;
		}

		public static T Read<T>(MemoryStream memory, SuperSerializer serializer = null, T existing = default)
		{
			serializer ??= new SuperSerializer();
			memory.Position = 0;

			using var input = new TokenInputStream(memory, true);
			return serializer.Read(input, existing);
		}

		public static T RoundTrip<T>(T value, SuperSerializer serializer = null)
		{
			using var memory = Write(value, serializer);
			return Read<T>(memory, serializer);
		}

		/// <summary>
		/// Writes with one serializer and reads with a brand new one, the way a fresh process would.
		/// </summary>
		public static T RoundTripFresh<T>(T value, Action<SuperSerializer> configureReader = null)
		{
			using var memory = Write(value);
			var reader = new SuperSerializer();
			configureReader?.Invoke(reader);
			return Read<T>(memory, reader);
		}

		/// <summary>
		/// The bytes after the stream header.
		/// </summary>
		public static byte[] Body(MemoryStream memory)
		{
			var bytes = memory.ToArray();
			var body = new byte[bytes.Length - TokenOutputStream.HEADER_SIZE];
			Array.Copy(bytes, TokenOutputStream.HEADER_SIZE, body, 0, body.Length);
			return body;
		}
	}
}
