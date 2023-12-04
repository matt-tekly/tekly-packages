using System;

namespace Tekly.Sheets.Dynamics
{
	public class DynamicConverterGeneric : DynamicConverter
	{
		private readonly Type m_type;
		private readonly TypeData m_typeData;
		
		public DynamicConverterGeneric(Type type)
		{
			m_type = type;
			m_typeData = new TypeData(m_type);
		}
		
		public override object Convert(DynamicSerializer serializer, Type type, object dyn, object existing)
		{
			existing ??= serializer.Create(m_type);

			var dynamic = dyn as Dynamic;
			foreach (var kvp in dynamic) {
				m_typeData.Set((string) kvp.Key, serializer, existing, kvp.Value);
			}

			return existing;
		}
	}
}