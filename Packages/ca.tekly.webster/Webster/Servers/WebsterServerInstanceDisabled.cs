//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using Tekly.Webster.Routing;

namespace Tekly.Webster.Servers
{
    public class WebsterServerInstanceDisabled : IWebsterServerInstance
    {
        public void AddRouteHandler(IRouteHandler routeHandler) { }

        public void AddRouteHandler<T>() where T : class, new() { }

        public void MainThreadUpdate()
        {
            throw new NotImplementedException();
        }

        public void Start(bool startFrameline)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public RoutesInfo GetRoutes()
        {
            throw new NotImplementedException();
        }

        public void Dispatch(Action action, long timeOutMs, int sleepTimeMs)
        {
            throw new NotImplementedException();
        }

        public T Dispatch<T>(Func<T> func, long timeOutMs, int sleepTimeMs) where T : class
        {
            return default;
        }
    }
}