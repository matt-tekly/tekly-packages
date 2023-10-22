using System;
using System.Collections;
using System.Collections.Generic;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Serialization
{
	public class SerialConverterList : SerialConverter
	{
		private readonly Type m_listType;
		private readonly Type m_valueType;
		private readonly TypeCode m_valueTypeCode;
		
		public SerialConverterList(Type type)
		{
			m_listType = type;
			m_valueType = type.GetGenericArguments()[0];
			m_valueTypeCode = Type.GetTypeCode(m_valueType);
		}
		
		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object obj)
		{
			var list = (IList) obj;
			var length = list.Count;

			stream.Write(length);
			
			for (var i = 0; i < length; i++) {
				var item = list[i];
				SerialConverterUtils.WriteValue(serializer, stream, item, m_valueTypeCode);
			}
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			var list = (IList) existing ?? (IList) Activator.CreateInstance(m_listType);
			list.Clear();
			var length = stream.ReadInt();

			for (var i = 0; i < length; i++) {
				list.Add(SerialConverterUtils.ReadValue(serializer, stream, m_valueType, m_valueTypeCode));
			}

			return list;
		}
	}
	
	public class SerialConverterListInt : SerialConverter
	{
		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object obj)
		{
			var list = (List<int>) obj;
			var length = list.Count;

			stream.Write(length);
			
			for (var i = 0; i < length; i++) {
				stream.Write(list[i]);
			}
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			var length = stream.ReadInt();
			List<int> list;
			
			if (existing != null) {
				list = (List<int>) existing;
				list.Clear();
				list.Capacity = length;
			} else {
				list = new List<int>(length);	
			}
			
			for (var i = 0; i < length; i++) {
				list.Add(stream.ReadInt());
			}

			return list;
		}
	}
}