using System;
using System.Net;
using System.Net.Sockets;
using Tekly.Tinker.Core;
using UnityEngine;

namespace Tekly.Tinker.Http
{
	public class HttpServer
	{
		public readonly int Port;
		
		private HttpListener m_listener;
		private Action<HttpListenerContext> m_requestHandler;

		public HttpServer(int port, Action<HttpListenerContext> requestHandler)
		{
			Port = port;
			m_requestHandler = requestHandler;
		}

		public void Start()
		{
			m_listener = new HttpListener();
			m_listener.Prefixes.Add($"http://*:{Port}/");

			m_listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
			m_listener.Start();
			
			ListenAsync();
		}
		
		public void Stop()
		{
			if (m_listener != null && m_listener.IsListening) {
				m_listener.Stop();
				m_listener = null;
			}
		}
		
		public string GetLocalIP()
		{
			try {
				using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);

				socket.Connect("8.8.8.8", 65530);

				var endPoint = socket.LocalEndPoint as IPEndPoint;
				return $"{endPoint.Address}:{Port}";
			} catch (Exception e) {
				Debug.LogError("Failed to get IP");
				Debug.LogException(e);
			}

			return "[Failed to get IP]";
		}
		
		private async void ListenAsync()
		{
			while (m_listener != null && m_listener.IsListening) {
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
				m_requestHandler?.Invoke(context);
			} catch (Exception e) {
				Debug.LogException(e);
				context.Response.StatusCode = 500;
				context.Response.WriteText(e.ToString());
			}
		}
	}
}