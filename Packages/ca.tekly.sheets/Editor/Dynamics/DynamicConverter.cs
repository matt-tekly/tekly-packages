using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Sheets.Dynamics
{
	public abstract class DynamicConverter
	{
		public abstract object Convert(DynamicSerializer serializer, Type type, object dyn, object existing);
		
		public virtual bool CanConvert(Type type)
		{
			return false;
		}
	}
	
	public class DynamicConverterDatabase
	{
		private readonly Dictionary<Type, DynamicConverter> m_converters = new Dictionary<Type, DynamicConverter>();
		private readonly List<DynamicConverter> m_generalConverters = new List<DynamicConverter>();
		
		private readonly Dictionary<Type, DynamicConverter> m_genericConverters = new Dictionary<Type, DynamicConverter>();

		public DynamicConverterDatabase()
		{
			Register(typeof(Vector2), new DynamicConverterVector2());
			Register(typeof(Vector3), new DynamicConverterVector3());
			Register(typeof(Quaternion), new DynamicConverterQuaternion());
		}
		
		public DynamicConverter Get(Type type)
		{
			if (!m_converters.TryGetValue(type, out var converter)) {
				converter = GetGeneralConverter(type);
				m_converters[type] = converter;
			}

			return converter;
		}
		
		public void Register(Type type, DynamicConverter converter)
		{
			m_converters[type] = converter;
		}

		public void Register(DynamicConverter converter)
		{
			m_generalConverters.Add(converter);
		}

		public DynamicConverter GetGenericConverter(Type type)
		{
			if (!m_genericConverters.TryGetValue(type, out var converter)) {
				converter = new DynamicConverterGeneric(type);
				m_genericConverters.Add(type, converter);
			}

			return converter;
		}

		private DynamicConverter GetGeneralConverter(Type type)
		{
			for (var index = m_generalConverters.Count - 1; index >= 0; index--) {
				var generalConverter = m_generalConverters[index];
				if (generalConverter.CanConvert(type)) {
					return generalConverter;
				}
			}

			if (type.IsArray) {
				return new DynamicConverterArray(type);
			}
			
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
				return new DynamicConverterList(type);
			}
			
			return new DynamicConverterGeneric(type);
		}
	}
}