//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Collections.Generic;
using System.Net;
using Tekly.Webster.Utility;
using UnityEngine;
using UnityEngine.Scripting;

namespace Tekly.Webster.Routing
{
	[Preserve]
	public class ClassRouteHandler : IRouteHandler
	{
		private readonly List<RouteDescriptor> m_routes = new List<RouteDescriptor>();
		private object m_context;

		public ClassRouteHandler(object context)
		{
			Initialize(context);
		}

		protected ClassRouteHandler()
		{
			Initialize(this);
		}

		[Preserve]
		public IEnumerable<RouteDescriptor> GetRouteDescriptors()
		{
			return m_routes;
		}

#if WEBSTER_ENABLE || UNITY_EDITOR && WEBSTER_ENABLE_EDITOR
		[Preserve]
		public bool TryHandleRoute(string route, HttpListenerRequest request, HttpListenerResponse response)
		{
			foreach (var routeDesc in m_routes) {
				if (routeDesc.Verb != request.HttpMethod || routeDesc.Path != route) {
					continue;
				}

				try {
					var invokeParams = RouteDescriptor.GetInvokeParams(routeDesc, request, response);
					object result;

					if (routeDesc.IsMainThreadDefault) {
						result = WebsterServer.Dispatch(() => routeDesc.Method.Invoke(m_context, invokeParams));
					} else {
						result = routeDesc.Method.Invoke(m_context, invokeParams);
					}

					response.Headers.Add("Access-Control-Allow-Origin", "*");

					switch (routeDesc.ReturnType) {
						case "void":
							break;
						case "string":
							response.WriteText(result.ToString());
							break;
						case "object":
							response.WriteJson(result ?? new object());
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				} catch (Exception ex) {
					Debug.LogException(ex);
				}

				return true;
			}

			return false;
		}
#endif

		[Preserve]
		protected void Initialize(object context)
		{
			m_context = context;
			var methods = m_context.GetType().GetMethods();

			var topLevelRoute = m_context.GetType().GetCustomAttribute<RouteAttribute>();
			var requestThread = m_context.GetType().GetCustomAttribute<RequestMainThreadAttribute>();

			var root = topLevelRoute != null ? topLevelRoute.Route ?? "" : "";
			var isMainThreadDefault = requestThread?.IsMainThread ?? true;

			foreach (var method in methods) {
				var route = method.GetCustomAttribute<RouteAttribute>();

				if (route == null) {
					continue;
				}

				try {
					var routeDescriptor = RouteDescriptor.FromMethod(method, root, isMainThreadDefault);
					m_routes.Add(routeDescriptor);
				} catch (Exception exception) {
					Debug.LogError($"Failed to create RouteDescriptor for method [{method.ReflectedType.Name}.{method.Name}]");
					Debug.LogException(exception);
				}
			}
		}
	}
}