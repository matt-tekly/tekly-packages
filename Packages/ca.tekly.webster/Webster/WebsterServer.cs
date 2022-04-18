//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

#if UNITY_EDITOR && WEBSTER_ENABLE_EDITOR
#define WEBSTER_ENABLE
#endif

using System;
using Tekly.Common.LifeCycles;
using Tekly.Webster.Routing;
using Tekly.Webster.Servers;
using UnityEngine.Scripting;

#if UNITY_2018_3_OR_NEWER
[assembly: AlwaysLinkAssembly]
#endif

namespace Tekly.Webster
{
    [Preserve]
    public class WebsterServer
    {
        public static long TimeOutMs = 5000;
        public static int SleepTimeMs = 16;

        public static int HttpPort = 4649;

        internal static IWebsterServerInstance Instance { get; private set; }

        /// <summary>
        /// This must be called from the Main Thread
        /// </summary>
#if !WEBSTER_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
        public static void Start(bool startFrameline)
        {
            LifeCycle.Instance.Update += MainThreadUpdate;
            LifeCycle.Instance.Quit += Stop;

            Instance = CreateInstance();
            Instance.Start(startFrameline);
        }

        public static void Stop()
        {
#if WEBSTER_ENABLE
            LifeCycle.Instance.Update -= MainThreadUpdate;
            LifeCycle.Instance.Quit -= Stop;

            Instance?.Stop();
            Instance = null;
#endif
        }

#if !WEBSTER_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
        public static void AddRouteHandler<T>() where T : class, new()
        {
            Instance.AddRouteHandler(new ClassRouteHandler(new T()));
        }

#if !WEBSTER_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
        public static void AddRouteHandler(IRouteHandler routeHandler)
        {
            Instance.AddRouteHandler(routeHandler);
        }

        /// <summary>
        /// Executes a function on the main thread.
        /// NOTE: This will block the thread it is called from.
        /// </summary>
#if !WEBSTER_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
        public static void Dispatch(Action action)
        {
            Instance.Dispatch(action, TimeOutMs, SleepTimeMs);
        }

        /// <summary>
        /// Executes a function on the main thread and returns the result.
        /// NOTE: This will block the thread it is called from.
        /// </summary>
        public static T Dispatch<T>(Func<T> func) where T : class
        {
            return Instance.Dispatch(func, TimeOutMs, SleepTimeMs);
        }

        public static void MainThreadUpdate()
        {
#if WEBSTER_ENABLE
            Instance.MainThreadUpdate();
#endif
        }

        private static IWebsterServerInstance CreateInstance()
        {
#if WEBSTER_ENABLE
            return new WebsterServerInstance();
#else
			return new WebsterServerInstanceDisabled();
#endif
        }
    }
}