using System;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Serialization
{
	public class SerialConverterArray : SerialConverter
	{
		private readonly Type m_arrayType;
		private readonly Type m_valueType;
		private readonly TypeCode m_valueTypeCode;

		public SerialConverterArray(Type type)
		{
			m_arrayType = type;
			m_valueType = m_arrayType.GetElementType();
			m_valueTypeCode = Type.GetTypeCode(m_valueType);
		}

		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object obj)
		{
			var array = (Array) obj;
			var length = array.Length;

			stream.Write(length);

			for (var i = 0; i < length; i++) {
				var item = array.GetValue(i);
				SerialConverterUtils.WriteValue(serializer, stream, item, m_valueTypeCode);
			}
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			var length = stream.ReadInt();
			var array = Array.CreateInstance(m_valueType, length);

			for (var i = 0; i < length; i++) {
				array.SetValue(SerialConverterUtils.ReadValue(serializer, stream, m_valueType, m_valueTypeCode), i);
			}

			return array;
		}
	}
}