using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Tekly.WebSockets.Core
{
	public class WebSocketServer
	{
		public Clients Clients => m_clients;
		
		private TcpListener m_tcpListener;
		private Thread m_listenerThread;
		private TcpClient m_connectedClient;
		private NetworkStream m_clientStream;

		public int Port { get; private set; }

		private readonly Clients m_clients = new Clients();
		
		private bool m_listening;
		
		public void Start(int port)
		{
			Port = port;
			m_listenerThread = new Thread(ListenForClients);
			m_listenerThread.IsBackground = true;
			m_listenerThread.Start();
		}

		private void ListenForClients()
		{
			m_listening = true;
			m_tcpListener = new TcpListener(IPAddress.Any, Port);
			m_tcpListener.Start();
			
			try {
				while (m_listening) {
					var client = m_tcpListener.AcceptTcpClient();
					m_clients.TryAdd(client);
				}
			} catch (SocketException) {
				// Do Nothing
			} catch (Exception exception) {
				Debug.LogException(exception);
			}
		}

		public void Stop()
		{
			m_clients.Stop();
			m_tcpListener?.Stop();

			m_listening = false;

			m_listenerThread = null;
			m_tcpListener = null;
		}
	}
}