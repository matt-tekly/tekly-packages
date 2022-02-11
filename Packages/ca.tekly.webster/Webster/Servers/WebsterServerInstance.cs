//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================
#if (WEBSTER_ENABLE || UNITY_EDITOR && WEBSTER_ENABLE_EDITOR)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Tekly.Webster.Assets;
using Tekly.Webster.Dispatching;
using Tekly.Webster.Routes;
using Tekly.Webster.Routes.Disk;
using Tekly.Webster.Routing;
using Tekly.Webster.Utility;
using UnityEngine;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;

namespace Tekly.Webster.Servers
{
	[Preserve]
	public class WebsterServerInstance : IWebsterServerInstance
	{
		private SimpleHttpServer m_simpleHttpServer;
		
		private AssetLoader m_assetLoader;
		
		private readonly List<IRouteHandler> m_routeHandlers = new List<IRouteHandler>();
		private readonly MainThreadDispatcher m_mainThreadDispatcher = new MainThreadDispatcher();
		
		public void AddRouteHandler(IRouteHandler routeHandler)
		{
			m_routeHandlers.Add(routeHandler);
		}
		
		public void AddRouteHandler<T>() where T : class, new()
		{
			AddRouteHandler(new ClassRouteHandler(new T()));
		}

		public void MainThreadUpdate()
		{
			m_mainThreadDispatcher.MainThreadUpdate();
			Frameline.Update();
		}
		
		/// <summary>
		/// This must be called from the Main Thread
		/// </summary>
		public void Start(bool startFrameline)
		{
			if (m_simpleHttpServer != null) {
				Debug.LogWarning("Trying to start Webster multiple times");
				return;
			}

			Debug.Log("Webster starting");

			m_assetLoader = new AssetLoader();
			AssetsEmbedded.Initialize(m_assetLoader);

			Application.runInBackground = true;
			WebsterBehaviour.CreateInstance();
			
			AddDefaultRoutes();

			m_mainThreadDispatcher.Initialize();

			m_simpleHttpServer = new SimpleHttpServer(WebsterServer.HttpPort);
			m_simpleHttpServer.OnRequest += OnRequest;
			m_simpleHttpServer.Start();
			
			Debug.Log("Webster initialized and started");

			if (startFrameline) {
				Frameline.Initialize();
			}
		}

		public void Stop()
		{
			m_simpleHttpServer?.Stop();
			Frameline.Stop();
		}
		
		public RoutesInfo GetRoutes()
		{
			return new RoutesInfo {
				Routes = m_routeHandlers.SelectMany(handler => handler.GetRouteDescriptors()).ToArray()
			};
		}
		
		public void Dispatch(Action action, long timeOutMs, int sleepTimeMs)
		{
			m_mainThreadDispatcher.Dispatch(action, timeOutMs, sleepTimeMs);
		} 
		
		public T Dispatch<T>(Func<T> func, long timeOutMs, int sleepTimeMs) where T : class
		{
			return m_mainThreadDispatcher.Dispatch(func, timeOutMs, sleepTimeMs);
		}
		
		private void AddDefaultRoutes()
		{
			AddRouteHandler<DefaultRoutes>();
			AddRouteHandler<PrefsRoutes>();
			AddRouteHandler<ScreenShotRoutes>();

			m_routeHandlers.Add(new DiskRoutes());
			m_routeHandlers.Add(new AssetsRoutes());
		}

		private void OnRequest(HttpListenerContext context)
		{
			try {
				var localPath = context.Request.Url.LocalPath;

				foreach (var routeHandler in m_routeHandlers) {
					if (routeHandler.TryHandleRoute(localPath, context.Request, context.Response)) {
						return;
					}
				}

				HandleFile(context.Request, context.Response);
			} catch (Exception exception) {
				context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
				context.Response.WriteText(exception.ToString());
			}
		}

		private void HandleFile(HttpListenerRequest request, HttpListenerResponse response)
		{
			var path = request.Url.AbsolutePath;

			if (path == "/") {
				path = "/index.html";
			}

			var result = m_assetLoader.GetResource(path);

			if (result.Found) {
				WriteResourceResult(response, result);
			} else {
				// If a requested file is not found we return a 404. If its not a file we just return index.html to
				// support how our single page application works.
				if (Path.HasExtension(path)) {
					response.StatusCode = 404;
					return;
				}

				result = m_assetLoader.GetResource("/index.html");
				WriteResourceResult(response, result);
			}
		}

		private static void WriteResourceResult(HttpListenerResponse response, GetResourceResult result)
		{
			ResponseUtility.SetResponseContent(result.Resource, response);
			if (result.Zipped) {
				response.AddHeader("Content-Encoding", "gzip");
			}

			response.WriteContent(result.Bytes);
		}
	}
}
#endif