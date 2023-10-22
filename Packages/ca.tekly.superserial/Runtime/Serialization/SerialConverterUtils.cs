using System;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Serialization
{
	public static class SerialConverterUtils
	{
		public static void WriteValue(SuperSerializer serializer, TokenOutputStream stream, object obj, TypeCode valueTypeCode)
		{
			switch (valueTypeCode) {
				case TypeCode.Boolean:
					stream.Write((bool) obj);
					break;
				case TypeCode.Double:
					stream.Write((double) obj);
					break;
				case TypeCode.Int32:
					stream.Write((int) obj);
					break;
				case TypeCode.UInt32:
					stream.Write((uint) obj);
					break;
				case TypeCode.Int64:
					stream.Write((long) obj);
					break;
				case TypeCode.Object:
					serializer.Write(stream, obj);
					break;
				case TypeCode.Single:
					stream.Write((float) obj);
					break;
				case TypeCode.String:
					stream.Write((string) obj);
					break;
				case TypeCode.DateTime:
					stream.Write((DateTime) obj);
					break;
				case TypeCode.Byte:
				case TypeCode.Char:
				case TypeCode.DBNull:
				case TypeCode.Decimal:
				case TypeCode.Empty:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.UInt64:
				case TypeCode.SByte:
					throw new Exception("Unsupported type " + valueTypeCode);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		
		public static object ReadValue(SuperSerializer serializer, TokenInputStream stream, Type valueType, TypeCode valueTypeCode)
		{
			switch (valueTypeCode) {
				case TypeCode.Boolean:
					return stream.ReadBoolean();
				case TypeCode.Double:
					return stream.ReadDouble();
				case TypeCode.Int32:
					return stream.ReadInt();
				case TypeCode.UInt32:
					return stream.ReadUInt();
				case TypeCode.Int64:
					return stream.ReadLong();
				case TypeCode.Object:
					return serializer.Read(stream, valueType, default);
				case TypeCode.Single:
					return stream.ReadFloat();
				case TypeCode.String:
					return stream.ReadString();
				case TypeCode.DateTime:
					return DateTime.FromBinary(stream.ReadLong());
				case TypeCode.Byte:
				case TypeCode.Char:
				case TypeCode.DBNull:
				case TypeCode.Decimal:
				case TypeCode.Empty:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.UInt64:
				case TypeCode.SByte:
					throw new Exception("Unsupported type " + valueTypeCode);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}