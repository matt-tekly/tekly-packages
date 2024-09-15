using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Tekly.Tinker.Routing
{
	public static class RouteUtils
	{
		public static object Parse(Type type, string value)
		{
			if (type.IsEnum) {
				return Enum.Parse(type, value);
			}
			
			if (IsNullable(type)) {
				return Parse(Nullable.GetUnderlyingType(type), value);
			}

			switch (Type.GetTypeCode(type)) {
				case TypeCode.Boolean:
					return bool.Parse(value);
				case TypeCode.Byte:
					return byte.Parse(value);
				case TypeCode.Char:
					return char.Parse(value);
				case TypeCode.DateTime:
					return DateTime.Parse(value);
				case TypeCode.Decimal:
					return decimal.Parse(value);
				case TypeCode.Double:
					return double.Parse(value);
				case TypeCode.Int16:
					return short.Parse(value);
				case TypeCode.Int32:
					return int.Parse(value);
				case TypeCode.Int64:
					return long.Parse(value);
				case TypeCode.SByte:
					return sbyte.Parse(value);
				case TypeCode.Single:
					return float.Parse(value);
				case TypeCode.String:
					return value;
				case TypeCode.UInt16:
					return ushort.Parse(value);
				case TypeCode.UInt32:
					return uint.Parse(value);
				case TypeCode.UInt64:
					return ulong.Parse(value);
				default:
					throw new Exception($"Trying to parse unknown type: {type.Name}");
			}
		}
		
		public static string NiceName(string value)
		{
			var output = Regex.Replace(value, "(\\B[A-Z])", " $1");
			return char.ToUpper(output[0]) + output.Substring(1);
		}

		public static string HumanName(Type type)
		{
			if (type.IsEnum) {
				return type.Name;
			}
			
			if (IsNullable(type)) {
				return HumanName(Nullable.GetUnderlyingType(type));
			}

			switch (Type.GetTypeCode(type)) {
				case TypeCode.Boolean:
					return "bool";
				case TypeCode.Byte:
					return "byte";
				case TypeCode.Char:
					return "char";
				case TypeCode.DateTime:
					return "date";
				case TypeCode.Decimal:
					return "decimal";
				case TypeCode.Double:
					return "double";
				case TypeCode.Int16:
					return "short";
				case TypeCode.Int32:
					return "int";
				case TypeCode.Int64:
					return "long";
				case TypeCode.SByte:
					return "sbyte";
				case TypeCode.Single:
					return "float";
				case TypeCode.String:
					return "string";
				case TypeCode.UInt16:
					return "ushort";
				case TypeCode.UInt32:
					return "uint";
				case TypeCode.UInt64:
					return "ulong";
				default:
					throw new Exception($"Trying to parse unknown type: {type.Name}");
			}
		}

		public static bool IsNullable(Type type)
		{
			return Nullable.GetUnderlyingType(type) != null;
		}

		public static bool IsNumber(Type type)
		{
			return IsInteger(type) || IsFloatingPoint(type);
		}
		
		public static bool IsInteger(Type type)
		{
			switch (Type.GetTypeCode(type)) {
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
					return true;
				default:
					return false;
			}
		}

		public static bool IsFloatingPoint(Type type)
		{
			switch (Type.GetTypeCode(type)) {
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
				default:
					return false;
			}
		}
		
		public static T GetAttribute<T>(this MemberInfo element) where T : Attribute
		{
			return (T) Attribute.GetCustomAttribute(element, typeof(T));
		}

		public static T GetAttribute<T>(this ParameterInfo element) where T : Attribute
		{
			return (T) Attribute.GetCustomAttribute(element, typeof(T));
		}

		public static bool Implements<T>(this Type type)
		{
			return typeof(T).IsAssignableFrom(type);
		}
	}
}