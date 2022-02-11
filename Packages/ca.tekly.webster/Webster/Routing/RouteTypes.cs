//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Reflection;
using UnityEngine.Scripting;

namespace Tekly.Webster.Routing
{
	/// <summary>
	/// The Route must always start with a '/'
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class RouteAttribute : PreserveAttribute
	{
		public readonly string Route;
		public readonly string Verb;

		public RouteAttribute(string route, string verb = null)
		{
			Route = route;
			Verb = verb;
		}
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class GetAttribute : RouteAttribute
	{
		public GetAttribute(string route) : base(route, "GET") { }
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class PostAttribute : RouteAttribute
	{
		public PostAttribute(string route) : base(route, "POST") { }
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class PutAttribute : RouteAttribute
	{
		public PutAttribute(string route) : base(route, "PUT") { }
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class DeleteAttribute : RouteAttribute
	{
		public DeleteAttribute(string route) : base(route, "DELETE") { }
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class DescriptionAttribute : PreserveAttribute
	{
		public readonly string Description;

		public DescriptionAttribute(string description)
		{
			Description = description;
		}

		public static string GetDescription(MemberInfo method)
		{
			var descriptionAttribute = method.GetCustomAttribute<DescriptionAttribute>();
			return descriptionAttribute != null ? descriptionAttribute.Description : "";
		}

		public static string GetDescription(ParameterInfo parameter)
		{
			var descriptionAttribute = parameter.GetCustomAttribute<DescriptionAttribute>();
			return descriptionAttribute != null ? descriptionAttribute.Description : "";
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class HiddenAttribute : PreserveAttribute { }

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class RequestMainThreadAttribute : PreserveAttribute
	{
		public readonly bool IsMainThread;

		public RequestMainThreadAttribute(bool isMainThread)
		{
			IsMainThread = isMainThread;
		}
	}
}