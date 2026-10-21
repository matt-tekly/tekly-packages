using System;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Serialization
{
	public static class SerialConverterPrimitives
	{
		public static void RegisterAll(SerialConverterDatabase database)
		{
			database.Register(new SerialConverterDelegate<bool>(WireType.Varint, (s, v) => s.WriteVarUInt(v ? 1u : 0u), s => s.ReadVarUInt() != 0));

			database.Register(new SerialConverterDelegate<sbyte>(WireType.Varint, (s, v) => s.WriteVarInt(v), s => checked((sbyte) s.ReadVarInt())));
			database.Register(new SerialConverterDelegate<byte>(WireType.Varint, (s, v) => s.WriteVarUInt(v), s => checked((byte) s.ReadVarUInt())));
			database.Register(new SerialConverterDelegate<short>(WireType.Varint, (s, v) => s.WriteVarInt(v), s => checked((short) s.ReadVarInt())));
			database.Register(new SerialConverterDelegate<ushort>(WireType.Varint, (s, v) => s.WriteVarUInt(v), s => checked((ushort) s.ReadVarUInt())));
			database.Register(new SerialConverterDelegate<int>(WireType.Varint, (s, v) => s.WriteVarInt(v), s => checked((int) s.ReadVarInt())));
			database.Register(new SerialConverterDelegate<uint>(WireType.Varint, (s, v) => s.WriteVarUInt(v), s => checked((uint) s.ReadVarUInt())));
			database.Register(new SerialConverterDelegate<long>(WireType.Varint, (s, v) => s.WriteVarInt(v), s => s.ReadVarInt()));
			database.Register(new SerialConverterDelegate<ulong>(WireType.Varint, (s, v) => s.WriteVarUInt(v), s => s.ReadVarUInt()));
			database.Register(new SerialConverterDelegate<char>(WireType.Varint, (s, v) => s.WriteVarUInt(v), s => checked((char) s.ReadVarUInt())));

			database.Register(new SerialConverterDelegate<float>(WireType.Fixed32, (s, v) => s.Write(v), s => s.ReadFloat()));
			database.Register(new SerialConverterDelegate<double>(WireType.Fixed64, (s, v) => s.Write(v), s => s.ReadDouble()));
			database.Register(new SerialConverterDelegate<DateTime>(WireType.Fixed64, (s, v) => s.Write(v), s => s.ReadDate()));
			database.Register(new SerialConverterDelegate<TimeSpan>(WireType.Fixed64, (s, v) => s.Write(v.Ticks), s => new TimeSpan(s.ReadLong())));

			database.Register(new SerialConverterDelegate<string>(WireType.Bytes, (s, v) => s.WriteUtf8(v), s => s.ReadUtf8()));

			database.Register(new SerialConverterDelegate<decimal>(WireType.Bytes, WriteDecimal, ReadDecimal));
			database.Register(new SerialConverterDelegate<Guid>(WireType.Bytes, WriteGuid, ReadGuid));
		}

		private static void WriteDecimal(TokenOutputStream stream, decimal value)
		{
			var bits = decimal.GetBits(value);
			stream.WriteVarUInt(sizeof(int) * 4);
			stream.Write(bits[0]);
			stream.Write(bits[1]);
			stream.Write(bits[2]);
			stream.Write(bits[3]);
		}

		private static decimal ReadDecimal(TokenInputStream stream)
		{
			if (!stream.ExpectLength(sizeof(int) * 4)) {
				return default;
			}

			return new decimal(new[] { stream.ReadInt(), stream.ReadInt(), stream.ReadInt(), stream.ReadInt() });
		}

		private static void WriteGuid(TokenOutputStream stream, Guid value)
		{
			Span<byte> bytes = stackalloc byte[16];
			value.TryWriteBytes(bytes);
			stream.WriteBytesWithLength(bytes);
		}

		private static Guid ReadGuid(TokenInputStream stream)
		{
			if (!stream.ExpectLength(16)) {
				return default;
			}

			Span<byte> bytes = stackalloc byte[16];
			stream.ReadBytes(bytes);
			return new Guid(bytes);
		}
	}
}
