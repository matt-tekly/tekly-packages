using System;

namespace Tekly.Sheets.Dynamics
{
	public class DynamicConverterArray : DynamicConverter
	{
		private readonly Type m_arrayType;
		private readonly Type m_valueType;
		private readonly TypeCode m_valueTypeCode;

		public DynamicConverterArray(Type type)
		{
			m_arrayType = type;
			m_valueType = type.GetElementType();
			m_valueTypeCode = Type.GetTypeCode(m_valueType);
		}

		public override object Convert(DynamicSerializer serializer, object dyn, object existing)
		{
			existing ??= serializer.Create(m_arrayType);
			var array = (Array) existing;

			var dynamic = dyn as Dynamic;
			
			array = ResizeList(array, dynamic.Count);

			var index = 0;
			foreach (var kvp in dynamic) {
				var oldValue = array.GetValue(index);
				var newValue = serializer.Deserialize(m_valueType, m_valueTypeCode, kvp.Value, oldValue);
				array.SetValue(newValue, index);
				index++;
			}

			return array;
		}

		private Array ResizeList(Array array, int size)
		{
			if (size == 0) {
				return Array.CreateInstance(m_valueType, 0);
			}

			var newArray = Array.CreateInstance(m_valueType, size);

			Array.Copy(array, newArray, Math.Min(array.Length, size));
			return newArray;
		}
	}
}