using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Tekly.WebSockets
{
	public class Clients
	{
		public event Action<Client> ClientConnected;
		public event Action<Client> ClientClosed;
		
		private readonly List<Client> m_clients = new List<Client>();
		private int m_nextId;
		
		private byte[] m_buffer = new byte[1024];
		
		public void TryAdd(TcpClient tcpClient)
		{
			var stream = tcpClient.GetStream();
			
			var bytesRead = stream.Read(m_buffer, 0, m_buffer.Length);
			
			var requestData = Encoding.UTF8.GetString(m_buffer, 0, bytesRead);

			var webSocketRequest = new WebSocketRequest(requestData);

			if (!webSocketRequest.IsValid) {
				Debug.LogError("Received invalid web socket request");
				tcpClient.Close();
				return;
			}
			
			var client = new Client(webSocketRequest, tcpClient, m_nextId++);
			m_clients.Add(client);

			client.Closed += OnClientClosed;
			
			ClientConnected?.Invoke(client);
		}

		public void Stop()
		{
			for (var index = m_clients.Count - 1; index >= 0; index--) {
				var client = m_clients[index];
				client.Closed -= OnClientClosed;
				client.Stop();
				
				ClientClosed?.Invoke(client);
			}
			
			m_clients.Clear();
		}

		private void OnClientClosed(Client client)
		{
			client.Closed -= OnClientClosed;
			m_clients.Remove(client);
			
			ClientClosed?.Invoke(client);
		}
	}
}