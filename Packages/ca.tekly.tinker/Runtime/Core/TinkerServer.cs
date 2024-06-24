#if UNITY_EDITOR
#define TINKER_ENABLED
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using DotLiquid;
using DotLiquid.NamingConventions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Tekly.Common.LifeCycles;
using Tekly.Common.Utils;
using Tekly.Tinker.Assets;
using Tekly.Tinker.Routes;
using Tekly.Tinker.Routing;
using UnityEngine;

namespace Tekly.Tinker.Core
{
	public class EndOfFrameAwaiter : INotifyCompletion
	{
		private Action _continuation;
		
		public bool IsCompleted => false;

		public void OnCompleted(Action continuation)
		{
			_continuation = continuation;
			LifeCycle.Instance.StartCoroutine(WaitForEndOfFrameCoroutine());
		}

		public void GetResult() { }

		private IEnumerator WaitForEndOfFrameCoroutine()
		{
			yield return new WaitForEndOfFrame();
			_continuation?.Invoke();
		}

		public EndOfFrameAwaiter GetAwaiter() => this;
	}
	
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
		
		private const int PORT = 3333;

		private int m_port;
		private HttpListener m_listener;

		private readonly List<ITinkerRoutes> m_routes = new List<ITinkerRoutes>();
		private const string TINKER_KEY = "Tinker";

		public TinkerData GetData(string url)
		{
			return new TinkerData(url, m_routes, Sidebar, AssetRoutes, Home);
		}

		public TinkerServer()
		{
			Serializer.Converters.Add(new StringEnumConverter());
		}
		
		public void Initialize(int port = PORT)
		{
			Application.runInBackground = true;
			m_port = port;

			try {
				m_listener = new HttpListener();
				m_listener.Prefixes.Add($"http://*:{port}/");

				m_listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
				m_listener.Start();

				ListenAsync();

				InitializeLiquid();
				InitializeSidebar();

				AddHandler(AssetRoutes);
				
				AddHandler(new TextureRoutes());
				AddHandler<TinkerPages>();
				AddHandler<UnityRoutes>();
				AddHandler<TinkerRpc>();

				LifeCycle.Instance.Quit += OnApplicationQuit;
			} catch (Exception e) {
				Debug.LogException(e);
				if (m_listener != null && m_listener.IsListening) {
					m_listener.Stop();
				}
			}
		}

		private void InitializeSidebar()
		{
			Sidebar.Section("Main")
				.Item("Home", "/");

			Sidebar.Section("Utility")
				.Item("Terminal", "/tinker/terminal");

			Home.Add("appinfo", "/tinker/info/app", 6, 5)
				.Add("assets", "/unity/assets/card", 4, 5);
		}

		public string GetLocalIP()
		{
			try {
				using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);

				socket.Connect("8.8.8.8", 65530);

				var endPoint = socket.LocalEndPoint as IPEndPoint;
				return $"{endPoint.Address}:{m_port}";
			} catch (Exception e) {
				Debug.LogError("Failed to get IP");
				Debug.LogException(e);
			}

			return "[Failed to get IP]";
		}

		public HtmlContent RenderTemplate(string templateName, object content, string contentKey = null)
		{
			var template = Template.Parse(AssetRoutes.ReadTemplateFile(templateName));

			var dict = new Dictionary<string, object>();
			dict[TINKER_KEY] = Hash.FromAnonymousObject(GetData(""));

			if (content == null) {
				return template.Render(Hash.FromDictionary(dict));
			}

			if (contentKey != null) {
				dict[contentKey] = content;
				var localHash = Hash.FromDictionary(dict);
				return template.Render(localHash);
			}

			var hash = Hash.FromDictionary(dict);
			hash.Merge(Hash.FromAnonymousObject(content));

			return template.Render(hash);
		}
		
		public HtmlContent RenderPage(string url, string templateName, string dataKey, object content)
		{
			var template = Template.Parse(AssetRoutes.ReadTemplateFile(templateName));

			var dict = new Dictionary<string, object>();
			dict[TINKER_KEY] = Hash.FromAnonymousObject(GetData(url));

			if (content == null) {
				return template.Render(Hash.FromDictionary(dict));
			}

			if (dataKey != null) {
				dict[dataKey] = content;
				var localHash = Hash.FromDictionary(dict);
				return template.Render(localHash);
			}

			var hash = Hash.FromDictionary(dict);
			hash.Merge(Hash.FromAnonymousObject(content));

			return template.Render(hash);
		}

		public HtmlContent RenderTemplate(PageAttribute page)
		{
			var template = Template.Parse(AssetRoutes.ReadTemplateFile(page.TemplateName));
			var dict = new Dictionary<string, object>();
			dict[TINKER_KEY] = Hash.FromAnonymousObject(GetData(page.Route));

			var tinkerHash = Hash.FromDictionary(dict);

			return template.Render(tinkerHash);
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
			var classRoutes = new ClassRoutes(new T(), this);
			m_routes.Add(classRoutes);

			return classRoutes;
		}

		public void RemoveHandler(ITinkerRoutes routes)
		{
			m_routes.Remove(routes);
		}

		private async void ListenAsync()
		{
			while (m_listener.IsListening) {
				try {
					var context = await m_listener.GetContextAsync();
					await new EndOfFrameAwaiter();
					ProcessRequest(context);
					context.Response.Close();
				} catch (ObjectDisposedException) { } catch (Exception ex) {
					Debug.LogException(ex);
				}
			}
		}

		private void ProcessRequest(HttpListenerContext context)
		{
			try {
				var route = context.Request.Url.LocalPath;
				
				foreach (var routeHandler in m_routes) {
					if (routeHandler.TryHandle(route, context.Request, context.Response)) {
						break;
					}
				}
			} catch (Exception e) {
				Debug.LogException(e);
				context.Response.StatusCode = 500;
				context.Response.WriteText(e.ToString());
			}
		}

		private void OnApplicationQuit()
		{
			if (m_listener != null && m_listener.IsListening) {
				m_listener.Stop();
			}
		}

		private void InitializeLiquid()
		{
			Template.FileSystem = AssetRoutes;
			Template.NamingConvention = new CSharpNamingConvention();
			Template.RegisterFilter(typeof(LiquidFilters));
		}
	}
}