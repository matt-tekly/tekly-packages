#if UNITY_EDITOR && TINKER_ENABLED_EDITOR
#define TINKER_ENABLED
#endif

#if TINKER_ENABLED
using System;
using System.Collections.Generic;
using System.Net;
using DotLiquid;
using DotLiquid.NamingConventions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Tekly.Tinker.Assets;
using Tekly.Tinker.Http;
using Tekly.Tinker.Routes;
using Tekly.Tinker.Routing;
using Tekly.WebSockets.Channeling;
using Tekly.WebSockets.Core;
using UnityEngine;

namespace Tekly.Tinker.Core
{
	public partial class TinkerServer : MonoBehaviour
	{
		public readonly JsonSerializer Serializer = new JsonSerializer();
		public readonly TinkerAssetRoutes AssetRoutes = new TinkerAssetRoutes();
		public readonly Sidebar Sidebar = new Sidebar();
		public readonly TinkerHome Home = new TinkerHome();

		public string LocalIP => m_httpServer?.GetLocalIP();

		public Channels Channels => m_channels;
		
		private readonly List<ITinkerRoutes> m_routes = new List<ITinkerRoutes>();
		
		private const int PORT_DEFAULT = 3333;
		private const string TINKER_DATA_KEY = "Tinker";

		private HttpServer m_httpServer;
		private WebSocketServer m_webSocketServer;
		private Channels m_channels;

		private static TinkerServer s_instance;
		public static TinkerServer Instance {
			get {
				if (s_instance == null) {
					GameObject tinkerGo = new GameObject();
					DontDestroyOnLoad(tinkerGo);
					s_instance = tinkerGo.AddComponent<TinkerServer>();
				}

				return s_instance;
			}
		}
		
		public void InitializeInternal(int port = PORT_DEFAULT)
		{
			port = HttpUtils.GetAvailablePort(port);
			
			Debug.Log($"Initializing Tinker at port [{port}]");
			
			Serializer.Converters.Add(new StringEnumConverter());
			m_webSocketServer = new WebSocketServer();
			m_channels = new Channels(m_webSocketServer.Clients);
			
			Application.runInBackground = true;
			
			try {
				m_httpServer = new HttpServer(port, ProcessRequest);
				m_httpServer.Start();

				var webSocketPort = HttpUtils.GetAvailablePort();
				m_webSocketServer.Start(webSocketPort);
				
				InitializeLiquid();
				InitializeContent();

				AddHandlerInternal(AssetRoutes);
				
				AddHandlerInternal(new TextureRoutes());
				AddHandlerInternal<TinkerPages>();
				AddHandlerInternal<UnityRoutes>();
			} catch (Exception e) {
				Debug.LogException(e);
				m_httpServer?.Stop();
			}
		}

		private void InitializeContent()
		{
			Sidebar.Section("Main")
				.Item("Home", "/");

			Sidebar.Section("Utility")
				.Item("Terminal", "/tinker/terminal");

			Home.Add("App Info", "/unity/info/app", 6, 10)
				.Add("Assets", "/unity/assets/card", 5, 5);
		}
		
		public HtmlContent RenderPage(string url, string templateName, string dataKey, object data)
		{
			var template = Template.Parse(AssetRoutes.ReadTemplateFile(templateName));

			var dict = new Dictionary<string, object>();
			dict[TINKER_DATA_KEY] = Hash.FromAnonymousObject(GetData(url));

			if (data == null) {
				return template.Render(Hash.FromDictionary(dict));
			}

			if (dataKey != null) {
				dict[dataKey] = data;
				var localHash = Hash.FromDictionary(dict);
				return template.Render(localHash);
			}

			var hash = Hash.FromDictionary(dict);
			hash.Merge(Hash.FromAnonymousObject(data));

			return template.Render(hash);
		}

		public void AddHandlerInternal(ITinkerRoutes routes)
		{
			m_routes.Add(routes);
		}
		
		public ITinkerRoutes AddClassHandlerInternal(object handler)
		{
			var classRoutes = new ClassRoutes(handler, this);
			m_routes.Add(classRoutes);

			return classRoutes;
		}

		public ITinkerRoutes AddHandlerInternal<T>() where T : new()
		{
			return AddClassHandler(new T());
		}

		public void RemoveHandlerInternal(ITinkerRoutes routes)
		{
			m_routes.Remove(routes);
		}
		
		private void ProcessRequest(HttpListenerContext context)
		{
			var route = context.Request.Url.LocalPath;
			
			foreach (var routeHandler in m_routes) {
				if (routeHandler.TryHandle(route, context.Request, context.Response)) {
					break;
				}
			}
		}
		
		private TinkerData GetData(string url)
		{
			return new TinkerData(url, m_routes, Sidebar, AssetRoutes, Home, m_webSocketServer.Port);
		}

		private void OnApplicationQuit()
		{
			s_instance = null;
			m_httpServer?.Stop();
			m_webSocketServer?.Stop();
		}

		private void InitializeLiquid()
		{
			Template.FileSystem = AssetRoutes;
			Template.NamingConvention = new CSharpNamingConvention();
			Template.RegisterFilter(typeof(LiquidFilters));
		}
	}
}
#endif