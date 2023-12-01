using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Sheets.Dynamics
{
	public abstract class DynamicConverter
	{
		public abstract object Convert(DynamicSerializer serializer, object dyn, object existing);
		
		public bool CanConvert(Type type)
		{
			return false;
		}
	}
	
	public class DynamicConverterDatabase
	{
		private readonly Dictionary<Type, DynamicConverter> m_converters = new Dictionary<Type, DynamicConverter>();
		private readonly List<DynamicConverter> m_generalConverters = new List<DynamicConverter>();

		public DynamicConverterDatabase()
		{
			Register(typeof(Vector3), new Vector3DynamicConverter());
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

		private DynamicConverter GetGeneralConverter(Type type)
		{
			foreach (var generalConverter in m_generalConverters) {
				if (generalConverter.CanConvert(type)) {
					return generalConverter;
				}
			}
			
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
				return new DynamicConverterList(type);
			}

			if (type.IsArray) {
				return new DynamicConverterArray(type);
			}

			return new GenericDynamicConverter(type);
		}
	}
}