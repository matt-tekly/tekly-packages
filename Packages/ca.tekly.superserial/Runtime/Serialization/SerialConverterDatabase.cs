using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.SuperSerial.Serialization
{
	public class SerialConverterDatabase
	{
		private readonly Dictionary<Type, SerialConverter> m_converters = new Dictionary<Type, SerialConverter>();

		public SerialConverterDatabase()
		{
			Register(typeof(List<int>), new SerialConverterListInt());
			
			Register(typeof(Vector2), new SerialConverterVector2());
			Register(typeof(Vector3), new SerialConverterVector3());
			Register(typeof(Quaternion), new SerialConverterQuaternion());
		}
		
		public SerialConverter Get(Type type)
		{
			if (!m_converters.TryGetValue(type, out var writer)) {
				writer = CreateConverter(type);
				m_converters.Add(type, writer);
			}

			return writer;
		}

		public void Register(Type type, SerialConverter converter)
		{
			m_converters[type] = converter;
		}

		private SerialConverter CreateConverter(Type type)
		{
			if (type.IsGenericType) {
				if (type.GetGenericTypeDefinition() == typeof(List<>)) {
					return new SerialConverterList(type);
				}
			}

			if (type.IsArray) {
				return new SerialConverterArray(type);
			}

			return new SerialConverterGeneric(type);
		}
	}
}