//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Linq;
using System.Net;
using System.Reflection;
using Tekly.Common.Utils;
using UnityEngine.Scripting;

namespace Tekly.Webster.Routing
{
	[Preserve]
	public class RoutesInfo
	{
		public RouteDescriptor[] Routes;
	}

	[Serializable]
	public class RouteDescriptor
	{
		public string Path;
		public string Verb;
		public string ReturnType;

		public ValueDescriptor[] QueryParams;

		public string Description;
		public bool Hidden;

		public bool IsMainThreadDefault;

		[NonSerialized] public MethodInfo Method;

		public static RouteDescriptor FromMethod(MethodInfo method, string root, bool isMainThreadDefault)
		{
			var route = method.GetAttribute<RouteAttribute>();

			var desc = new RouteDescriptor();
			desc.Method = method;
			desc.Path = root + route.Route;
			desc.Verb = route.Verb;
			desc.ReturnType = RouteUtility.GetReturnType(method.ReturnType);
			desc.Hidden = method.GetAttribute<HiddenAttribute>() != null;
			desc.Description = DescriptionAttribute.GetDescription(method);
			desc.QueryParams = method.GetParameters()
				.Select(ValueDescriptor.FromParameter)
				.ToArray();

			var requestThreadAttribute = method.GetAttribute<RequestMainThreadAttribute>();
			desc.IsMainThreadDefault = requestThreadAttribute != null ? requestThreadAttribute.IsMainThread : isMainThreadDefault;

			return desc;
		}

		public static object[] GetInvokeParams(RouteDescriptor descriptor, HttpListenerRequest request, HttpListenerResponse response)
		{
			var queryParams = request.QueryString;
			var invokeParams = new object[descriptor.QueryParams.Length];

			for (var index = 0; index < descriptor.QueryParams.Length; index++) {
				var valueDescriptor = descriptor.QueryParams[index];

				if (valueDescriptor.ActualType == typeof(HttpListenerRequest)) {
					invokeParams[index] = request;
					continue;
				}

				if (valueDescriptor.ActualType == typeof(HttpListenerResponse)) {
					invokeParams[index] = response;
					continue;
				}

				var value = queryParams.Get(valueDescriptor.Name);

				if (value != null) {
					invokeParams[index] = TypeUtility.Parse(valueDescriptor.ActualType, value);
				} else if (valueDescriptor.Optional) {
					invokeParams[index] = valueDescriptor.DefaultValue;
				} else {
					throw new Exception($"Query missing required parameter: {valueDescriptor.Name}");
				}
			}

			return invokeParams;
		}
	}

	[Serializable]
	public class ValueDescriptor
	{
		[NonSerialized] public Type ActualType;

		public string DefaultValue;
		public string Description;
		public string Name;
		public bool Optional;
		public string Type;
		public string[] Values;

		public static ValueDescriptor FromParameter(ParameterInfo param)
		{
			var descriptor = new ValueDescriptor();
			descriptor.ActualType = param.ParameterType;
			descriptor.Name = param.Name;
			descriptor.Type = RouteUtility.GetValueType(param);
			descriptor.DefaultValue = param.DefaultValue != null ? param.DefaultValue.ToString() : "";
			descriptor.Optional = param.IsOptional || TypeUtility.IsNullable(param.ParameterType);
			descriptor.Description = DescriptionAttribute.GetDescription(param);

			if (param.ParameterType.IsEnum) {
				descriptor.Values = Enum.GetNames(param.ParameterType);
				if (string.IsNullOrEmpty(descriptor.DefaultValue)) {
					descriptor.DefaultValue = descriptor.Values[0];
				}
			}

			return descriptor;
		}
	}
}