using System;
using System.Collections;

namespace Tekly.Sheets.Dynamics
{
	public class DynamicConverterList : DynamicConverter
	{
		private readonly Type m_listType;
		private readonly Type m_valueType;
		private readonly TypeCode m_valueTypeCode;

		public DynamicConverterList(Type type)
		{
			m_listType = type;
			m_valueType = type.GetGenericArguments()[0];
			m_valueTypeCode = Type.GetTypeCode(m_valueType);
		}

		public override object Convert(DynamicSerializer serializer, Type type, object dyn, object existing)
		{
			existing ??= serializer.Create(m_listType);
			var list = (IList) existing;

			var dynamic = dyn as Dynamic;

			ResizeList(list, dynamic.Count);

			var index = 0;
			foreach (var kvp in dynamic) {
				list[index] = serializer.Deserialize(m_valueType, m_valueTypeCode, kvp.Value, list[index]);
				index++;
			}

			return existing;
		}

		private void ResizeList(IList list, int size)
		{
			if (size == 0) {
				list.Clear();
				return;
			}

			while (list.Count > size) {
				list.RemoveAt(list.Count - 1);
			}

			while (list.Count < size) {
				if (m_valueType.IsValueType) {
					list.Add(Activator.CreateInstance(m_valueType));
				} else {
					list.Add(null);
				}
			}
		}
	}
}