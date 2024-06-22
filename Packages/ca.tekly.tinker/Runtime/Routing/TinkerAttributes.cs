#if UNITY_EDITOR
#define TINKER_ENABLED
#endif
using System;
using UnityEngine.Scripting;

namespace Tekly.Tinker.Core
{
#if TINKER_ENABLED
	public class BaseAttribute : PreserveAttribute { }
#else
	public class BaseAttribute : Attribute { }
#endif

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class RouteAttribute : BaseAttribute
	{
		public readonly string Route;
		public readonly string Verb;

		public RouteAttribute(string route, string verb = null)
		{
			Route = route;
			Verb = verb;
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class PageAttribute : GetAttribute
	{
		public readonly string TemplateName;
		public readonly string DataKey;

		public PageAttribute(string route, string templateName, string dataKey = null) : base(route)
		{
			TemplateName = templateName;
			DataKey = dataKey;
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class GetAttribute : RouteAttribute
	{
		public GetAttribute(string route) : base(route, "GET") { }
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class PostAttribute : RouteAttribute
	{
		public PostAttribute(string route) : base(route, "POST") { }
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class PutAttribute : RouteAttribute
	{
		public PutAttribute(string route) : base(route, "PUT") { }
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class DeleteAttribute : RouteAttribute
	{
		public DeleteAttribute(string route) : base(route, "DELETE") { }
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class DescriptionAttribute : Attribute
	{
		public readonly string Name;
		public readonly string Description;

		public DescriptionAttribute(string name, string description = null)
		{
			Name = name;
			Description = description;
		}
	}
	
	[AttributeUsage(AttributeTargets.Parameter)]
	public class LargeTextAttribute : Attribute
	{
	}
}