#if UNITY_EDITOR
#define TINKER_ENABLED
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using DotLiquid;
using Newtonsoft.Json;
using Tekly.Common.LifeCycles;
using Tekly.Common.Utils;
using Tekly.Tinker.Assets;
using Tekly.Tinker.Routing;
using UnityEngine;

namespace Tekly.Tinker.Core
{
	public class TinkerDrop
	{
		public List<ClassRoutes> Routes;
		public Vector3 Position = new Vector3(1, 2, 3);
		public Vector2 Position2 = new Vector3(1, 2, 3);
		
		public TinkerDrop(List<ITinkerRoutes> routes)
		{
			Routes = routes.OfType<ClassRoutes>()
				.Where(x => x.Visible)
				.ToList();
		}
	}
	
	public class TinkerServer : Singleton<TinkerServer>
	{
		public readonly JsonSerializer Serializer = new JsonSerializer();
		public readonly TinkerAssetRoutes AssetRoutes = new TinkerAssetRoutes();

		public TinkerDrop Drop => new TinkerDrop(m_routes);
		
		private const int PORT = 3333;

		private int m_port;
		private HttpListener m_listener;

		private readonly List<ITinkerRoutes> m_routes = new List<ITinkerRoutes>();

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
					
				AddHandler(AssetRoutes);
				AddHandler(new TextureRoutes());
				AddHandler<TinkerPages>();

				LifeCycle.Instance.Quit += OnApplicationQuit;
			} catch (Exception e) {
				Debug.LogException(e);
				if (m_listener != null && m_listener.IsListening) {
					m_listener.Stop();
				}
			}
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
			dict["tinker"] = Hash.FromAnonymousObject(Drop);
			
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

		public HtmlContent RenderTemplate(string templateName)
		{
			var template = Template.Parse(AssetRoutes.ReadTemplateFile(templateName));
			var tinkerHash = Hash.FromAnonymousObject(Drop);
			
			return template.Render(tinkerHash);
		}

		public void AddHandler(ITinkerRoutes routes)
		{
			m_routes.Add(routes);
		}

		public void AddHandler<T>() where T : new()
		{
			m_routes.Add(new ClassRoutes(new T(), this));
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

				if (route == "/tinker/routes") {
					context.Response.WriteHtml(RenderTemplate("tinker_routes"));
					return;
				}

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
			Template.RegisterFilter(typeof(LiquidFilters));
			
			Template.RegisterSafeType(typeof(TestDrop), new [] { "*" });
			Template.RegisterSafeType(typeof(Vector3), new [] { "*" });
			Template.RegisterSafeType(typeof(Vector3), new [] { "*" });
		}
	}
}