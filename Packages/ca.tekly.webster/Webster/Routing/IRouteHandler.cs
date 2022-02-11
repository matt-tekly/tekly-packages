//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System.Collections.Generic;
using System.Net;

namespace Tekly.Webster.Routing
{
	public interface IRouteHandler
	{
#if WEBSTER_ENABLE || UNITY_EDITOR && WEBSTER_ENABLE_EDITOR
		bool TryHandleRoute(string route, HttpListenerRequest request, HttpListenerResponse response);
#endif
		IEnumerable<RouteDescriptor> GetRouteDescriptors();
	}
}