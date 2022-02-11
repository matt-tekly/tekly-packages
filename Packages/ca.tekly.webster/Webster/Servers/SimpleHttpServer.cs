//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Net;
using System.Threading;
using UnityEngine;

namespace Tekly.Webster.Servers
{
	public class SimpleHttpServer
	{
		private readonly HttpListener m_httpListener;
		private Thread m_thread;

		public event Action<HttpListenerContext> OnRequest;
		
		public SimpleHttpServer(int port, string hostName = "*", bool secure = false)
		{
			m_httpListener = new HttpListener();
			var uriPrefix = $"{(secure ? "https" : "http")}://{hostName}:{port}/";
			m_httpListener.Prefixes.Add(uriPrefix);
			m_httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
		}

		public void Start()
		{
			m_httpListener.Start();
			m_thread = new Thread(Listen);
			m_thread.Start();
		}

		private void Listen()
		{
			while (true) {
				if (m_httpListener.IsListening) {
					var result = m_httpListener.BeginGetContext(ListenerCallback, m_httpListener);
					result.AsyncWaitHandle.WaitOne();	
				} else {
					break;
				}
			}
		}

		private void ListenerCallback(IAsyncResult result)
		{
			var context = m_httpListener.EndGetContext(result);

			try {
				OnRequest?.Invoke(context);
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
			
			context.Response.OutputStream.Close();
			context.Response.Close();
		}

		public void Stop()
		{
			m_httpListener.Stop();
			m_thread.Join(1000);
		}
	}
}