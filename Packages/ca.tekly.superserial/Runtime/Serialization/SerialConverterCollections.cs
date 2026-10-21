using System;
using System.Collections;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Serialization
{
	public class SerialConverterList : SerialConverter
	{
		private readonly Type m_listType;
		private readonly Type m_elementType;

		public override WireType WireType => WireType.List;

		public SerialConverterList(SerialConverterDatabase database, Type listType)
		{
			m_listType = listType;
			m_elementType = listType.GetGenericArguments()[0];
			RegisterElementType(database, m_elementType);
		}

		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object value)
		{
			var list = (IList) value;
			var count = list.Count;

			stream.WriteVarUInt((ulong) count);

			for (var i = 0; i < count; i++) {
				serializer.WriteValue(stream, m_elementType, list[i]);
			}
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			var list = existing as IList ?? (IList) Activator.CreateInstance(m_listType);
			list.Clear();

			var count = stream.ReadVarUInt();

			for (ulong i = 0; i < count; i++) {
				list.Add(serializer.ReadValue(stream, m_elementType, null));
			}

			return list;
		}

		internal static void RegisterElementType(SerialConverterDatabase database, Type elementType)
		{
			if (!elementType.IsAbstract && !elementType.IsInterface) {
				database.RegisterType(elementType);
			}
		}
	}

	public class SerialConverterArray : SerialConverter
	{
		private readonly Type m_elementType;

		public override WireType WireType => WireType.List;

		public SerialConverterArray(SerialConverterDatabase database, Type arrayType)
		{
			m_elementType = arrayType.GetElementType();
			SerialConverterList.RegisterElementType(database, m_elementType);
		}

		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object value)
		{
			var array = (Array) value;
			var length = array.Length;

			stream.WriteVarUInt((ulong) length);

			for (var i = 0; i < length; i++) {
				serializer.WriteValue(stream, m_elementType, array.GetValue(i));
			}
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			var length = (int) stream.ReadVarUInt();

			var array = existing as Array;
			if (array == null || array.Length != length) {
				array = Array.CreateInstance(m_elementType, length);
			}

			for (var i = 0; i < length; i++) {
				array.SetValue(serializer.ReadValue(stream, m_elementType, null), i);
			}

			return array;
		}
	}

	public class SerialConverterDictionary : SerialConverter
	{
		private readonly Type m_dictionaryType;
		private readonly Type m_keyType;
		private readonly Type m_valueType;

		public override WireType WireType => WireType.Map;

		public SerialConverterDictionary(SerialConverterDatabase database, Type dictionaryType)
		{
			m_dictionaryType = dictionaryType;

			var arguments = dictionaryType.GetGenericArguments();
			m_keyType = arguments[0];
			m_valueType = arguments[1];

			SerialConverterList.RegisterElementType(database, m_keyType);
			SerialConverterList.RegisterElementType(database, m_valueType);
		}

		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object value)
		{
			var dictionary = (IDictionary) value;

			stream.WriteVarUInt((ulong) dictionary.Count);

			foreach (DictionaryEntry entry in dictionary) {
				serializer.WriteValue(stream, m_keyType, entry.Key);
				serializer.WriteValue(stream, m_valueType, entry.Value);
			}
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			var dictionary = existing as IDictionary ?? (IDictionary) Activator.CreateInstance(m_dictionaryType);
			dictionary.Clear();

			var count = stream.ReadVarUInt();

			for (ulong i = 0; i < count; i++) {
				var key = serializer.ReadValue(stream, m_keyType, null);
				var value = serializer.ReadValue(stream, m_valueType, null);

				if (key == null) {
					throw new SuperSerialException($"Null key in serialized {m_dictionaryType}");
				}

				dictionary[key] = value;
			}

			return dictionary;
		}
	}

	public class SerialConverterByteArray : SerialConverter<byte[]>
	{
		public override WireType WireType => WireType.Bytes;

		public override void Write(SuperSerializer serializer, TokenOutputStream stream, byte[] value)
		{
			stream.WriteBytesWithLength(value);
		}

		public override byte[] Read(SuperSerializer serializer, TokenInputStream stream, byte[] existing)
		{
			var length = (int) stream.ReadVarUInt();

			if (existing == null || existing.Length != length) {
				existing = new byte[length];
			}

			stream.ReadBytes(existing);
			return existing;
		}
	}
}
