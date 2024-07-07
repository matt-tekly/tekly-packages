#if UNITY_EDITOR
#define TINKER_ENABLED
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DotLiquid;
using DotLiquid.NamingConventions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Tekly.Common.LifeCycles;
using Tekly.Common.Utils;
using Tekly.Tinker.Assets;
using Tekly.Tinker.Http;
using Tekly.Tinker.Routes;
using Tekly.Tinker.Routing;
using UnityEngine;

namespace Tekly.Tinker.Core
{
	public class TinkerData
	{
		public string Url;
		public List<ClassRoutes> Routes;
		public Sidebar Sidebar;
		public List<string> Css = new List<string>();
		public TinkerHome Home;

		public TinkerData(string url, List<ITinkerRoutes> routes, Sidebar sidebar, TinkerAssetRoutes assetRoutes, TinkerHome home)
		{
			Home = home;
			Url = url;
			Routes = routes.OfType<ClassRoutes>().ToList();
			Sidebar = sidebar;

			var cssAssets = new List<TinkerAsset>();
			assetRoutes.GetAssets("css", cssAssets);
			Css.AddRange(cssAssets.Select(x => x.Url));
		}
	}

	public class TinkerServer : Singleton<TinkerServer>
	{
		public readonly JsonSerializer Serializer = new JsonSerializer();
		public readonly TinkerAssetRoutes AssetRoutes = new TinkerAssetRoutes();
		public readonly Sidebar Sidebar = new Sidebar();
		public readonly TinkerHome Home = new TinkerHome();

		public string LocalIP => m_httpServer?.GetLocalIP();
		
		private readonly List<ITinkerRoutes> m_routes = new List<ITinkerRoutes>();
		
		private const int PORT_DEFAULT = 3333;
		private const string TINKER_DATA_KEY = "Tinker";

		private HttpServer m_httpServer;

		public TinkerServer()
		{
			Serializer.Converters.Add(new StringEnumConverter());
		}
		
		public void Initialize(int port = PORT_DEFAULT)
		{
			Application.runInBackground = true;
			
			try {
				m_httpServer = new HttpServer(port, ProcessRequest);
				m_httpServer.Start();
				
				InitializeLiquid();
				InitializeContent();

				AddHandler(AssetRoutes);
				
				AddHandler(new TextureRoutes());
				AddHandler<TinkerPages>();
				AddHandler<UnityRoutes>();

				LifeCycle.Instance.Quit += OnApplicationQuit;
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

			Home.Add("appinfo", "/unity/info/app", 6, 10)
				.Add("assets", "/unity/assets/card", 5, 5);
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

		public void AddHandler(ITinkerRoutes routes)
		{
			m_routes.Add(routes);
		}
		
		public ITinkerRoutes AddClassHandler(object handler)
		{
			var classRoutes = new ClassRoutes(handler, this);
			m_routes.Add(classRoutes);

			return classRoutes;
		}

		public ITinkerRoutes AddHandler<T>() where T : new()
		{
			return AddClassHandler(new T());
		}

		public void RemoveHandler(ITinkerRoutes routes)
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
			return new TinkerData(url, m_routes, Sidebar, AssetRoutes, Home);
		}

		private void OnApplicationQuit()
		{
			m_httpServer?.Stop();
		}

		private void InitializeLiquid()
		{
			Template.FileSystem = AssetRoutes;
			Template.NamingConvention = new CSharpNamingConvention();
			Template.RegisterFilter(typeof(LiquidFilters));
		}
	}
}