//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================
using System;
using Tekly.Webster.Routing;

namespace Tekly.Webster.Servers
{
	public interface IWebsterServerInstance
	{
		void AddRouteHandler(IRouteHandler routeHandler);
		void MainThreadUpdate();

		/// <summary>
		/// This must be called from the Main Thread
		/// </summary>
		void Start(bool startFrameline);

		void Stop();
		RoutesInfo GetRoutes();
		void Dispatch(Action action, long timeOutMs, int sleepTimeMs);
		T Dispatch<T>(Func<T> func, long timeOutMs, int sleepTimeMs) where T : class;
	}
}