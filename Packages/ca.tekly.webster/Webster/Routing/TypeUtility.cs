//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Net;
using System.Reflection;

namespace Tekly.Webster.Routing
{
	public static class TypeUtility
	{
		public static object Parse(ValueDescriptor valueDescriptor, string value)
		{
			if (valueDescriptor.ActualType.IsEnum) {
				return Enum.Parse(valueDescriptor.ActualType, value);
			}

			switch (Type.GetTypeCode(valueDescriptor.ActualType)) {
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
					throw new Exception(string.Format("Trying to parse unknown type: {0}", valueDescriptor.ActualType.Name));
			}
		}

		public static bool IsNullable(Type type)
		{
			return Nullable.GetUnderlyingType(type) != null;
		}

		public static string GetValueType(ParameterInfo parameterInfo)
		{
			var type = Nullable.GetUnderlyingType(parameterInfo.ParameterType) ?? parameterInfo.ParameterType;

			if (type.IsEnum) {
				return "enum";
			}

			if (IsInteger(type)) {
				return "long";
			}

			if (IsFloatingPoint(type)) {
				return "double";
			}

			if (type == typeof(bool)) {
				return "boolean";
			}

			if (type == typeof(string)) {
				return "string";
			}

			if (type == typeof(HttpListenerResponse)) {
				return "response";
			}
			
			if (type == typeof(HttpListenerRequest)) {
				return "request";
			}

			throw new Exception($"Route has parameter [{parameterInfo.Name}] with an invalid type [{type.Name}]. All parameters must be value types.");
		}
		
		private static bool IsInteger(Type type)
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

		private static bool IsFloatingPoint(Type type)
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

		public static string GetReturnType(Type type)
		{
			if (type == typeof(void)) {
				return "void";
			}

			if (type.IsValueType || typeof(string) == type || type.IsEnum) {
				return "string";
			}

			return "object";
		}

		public static T GetCustomAttribute<T>(this MemberInfo element) where T : Attribute
		{
			return (T) Attribute.GetCustomAttribute(element, typeof(T));
		}

		public static T GetCustomAttribute<T>(this ParameterInfo element) where T : Attribute
		{
			return (T) Attribute.GetCustomAttribute(element, typeof(T));
		}
	}
}