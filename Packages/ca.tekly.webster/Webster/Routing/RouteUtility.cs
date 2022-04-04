//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Net;
using System.Reflection;
using Tekly.Common.Utils;

namespace Tekly.Webster.Routing
{
	public static class RouteUtility
	{
		public static string GetValueType(ParameterInfo parameterInfo)
		{
			var type = Nullable.GetUnderlyingType(parameterInfo.ParameterType) ?? parameterInfo.ParameterType;

			if (type.IsEnum) {
				return "enum";
			}

			if (TypeUtility.IsInteger(type)) {
				return "long";
			}

			if (TypeUtility.IsFloatingPoint(type)) {
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
	}
}