using System;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Serialization
{
	/// <summary>
	/// Enums are written as a zigzag varint of their underlying value, so they share a wire type with every
	/// integer and an enum field can become an int field (or the reverse) without breaking saves.
	/// </summary>
	public class SerialConverterEnum : SerialConverter
	{
		private readonly Type m_enumType;
		private readonly TypeCode m_underlyingTypeCode;

		public override WireType WireType => WireType.Varint;

		public SerialConverterEnum(Type enumType)
		{
			m_enumType = enumType;
			m_underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(enumType));
		}

		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object value)
		{
			long raw;

			switch (m_underlyingTypeCode) {
				case TypeCode.SByte:
					raw = (sbyte) value;
					break;
				case TypeCode.Byte:
					raw = (byte) value;
					break;
				case TypeCode.Int16:
					raw = (short) value;
					break;
				case TypeCode.UInt16:
					raw = (ushort) value;
					break;
				case TypeCode.Int32:
					raw = (int) value;
					break;
				case TypeCode.UInt32:
					raw = (uint) value;
					break;
				case TypeCode.Int64:
					raw = (long) value;
					break;
				case TypeCode.UInt64:
					raw = unchecked((long) (ulong) value);
					break;
				default:
					throw new SuperSerialException($"Unsupported enum underlying type for {m_enumType}");
			}

			stream.WriteVarInt(raw);
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			return Enum.ToObject(m_enumType, stream.ReadVarInt());
		}
	}
}
