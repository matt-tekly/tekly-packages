using System;

namespace Tekly.Sheets.Dynamics
{
	public class DynamicSerializer
	{
		public readonly DynamicConverterDatabase Converters = new DynamicConverterDatabase();
		
		public object Deserialize(Type type, Dynamic dynamic, object existing)
		{
			var converter = Converters.Get(type);
			return converter.Convert(this, type, dynamic, existing);
		}
		
		public T Deserialize<T>(Dynamic dynamic, object existing)
		{
			var type = typeof(T);
			var converter = Converters.Get(type);

			return (T) converter.Convert(this, type, dynamic, existing);
		}
		
		public object Deserialize(Type type, object dynamic, object existing)
		{
			var converter = Converters.Get(type);
			return converter.Convert(this, type, dynamic, existing);
		}
		
		public object Deserialize(Type type, TypeCode typeCode, object dynamic, object existing)
		{
			if (typeCode == TypeCode.Object) {
				var converter = Converters.Get(type);
				return converter.Convert(this, type, dynamic, existing);	
			}

			return Convert(type, typeCode, dynamic);
		}
		
		public void Populate(Dynamic dynamic, object target)
		{
			var type = target.GetType();
			var converter = Converters.GetGenericConverter(type);
			converter.Convert(this, type, dynamic, target);
		}
		
		public object Create(Type type)
		{
			return Activator.CreateInstance(type);
		}
		
		public object Convert(Type type, TypeCode typeCode, object value)
		{
			if (value == null) {
				// TODO: Should this be treated as an error?
				return null;
			}
			
			if (type.IsEnum) {
				return Enum.Parse(type, value as string);
			}
			
			switch (typeCode) {
				case TypeCode.Boolean:
					return System.Convert.ToBoolean(value);
				case TypeCode.Byte:
					return System.Convert.ToByte(value);
				case TypeCode.Char:
					return System.Convert.ToChar(value);
				case TypeCode.DateTime:
					return System.Convert.ToDateTime(value);
				case TypeCode.Decimal:
					return System.Convert.ToDecimal(value);
				case TypeCode.Double:
					return System.Convert.ToDouble(value);
				case TypeCode.Int16:
					return System.Convert.ToInt16(value);
				case TypeCode.Int32:
					return System.Convert.ToInt32(value);
				case TypeCode.Int64:
					return System.Convert.ToInt64(value);
				case TypeCode.SByte:
					return System.Convert.ToSByte(value);
				case TypeCode.Single:
					return System.Convert.ToSingle(value);
				case TypeCode.String:
					return value;
				case TypeCode.UInt16:
					return System.Convert.ToUInt16(value);
				case TypeCode.UInt32:
					return System.Convert.ToUInt32(value);
				case TypeCode.UInt64:
					return System.Convert.ToUInt64(value);
				default:
					throw new Exception($"Trying to parse unknown type: {type.Name}");
			}
		}
	}
}