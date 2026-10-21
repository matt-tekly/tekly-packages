using System;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Serialization
{
	/// <summary>
	/// Converter for types that implement ISuperSerialize. Nested in another value the payload is a length
	/// prefixed Block so unknown data can still be skipped. At the top level of a stream it is written Raw.
	/// </summary>
	public class SerialConverterSuperSerialize : SerialConverter
	{
		private readonly Type m_type;

		public override WireType WireType => WireType.Block;

		public SerialConverterSuperSerialize(Type type)
		{
			m_type = type;
		}

		public override bool Accepts(WireType wireType)
		{
			return wireType == WireType.Block || wireType == WireType.Raw;
		}

		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object value)
		{
			((ISuperSerialize) value).Write(stream, serializer);
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			var obj = existing ?? serializer.CreateInstance(m_type);
			((ISuperSerialize) obj).Read(stream, serializer);
			return obj;
		}
	}
}
