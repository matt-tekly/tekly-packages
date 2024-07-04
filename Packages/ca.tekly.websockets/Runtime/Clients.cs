using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Tekly.WebSockets
{
	public class Clients
	{
		private readonly List<Client> m_clients = new List<Client>();
		private int m_nextId;
		
		public void TryAdd(TcpClient tcpClient)
		{
			var stream = tcpClient.GetStream();
			
			var buffer = new byte[1024];
			var bytesRead = stream.Read(buffer, 0, buffer.Length);
			
			var requestData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

			var webSocketRequest = new WebSocketRequest(requestData);

			if (!webSocketRequest.IsValid) {
				Debug.LogError("Received invalid web socket request");
				tcpClient.Close();
				return;
			}
			
			var client = new Client(webSocketRequest, tcpClient, m_nextId++);
			m_clients.Add(client);

			client.Closed += OnClientClosed;
		}

		public void Stop()
		{
			for (var index = m_clients.Count - 1; index >= 0; index--) {
				var connection = m_clients[index];
				connection.Closed -= OnClientClosed;
				connection.Stop();
			}
			
			m_clients.Clear();
		}

		private void OnClientClosed(Client client)
		{
			client.Closed -= OnClientClosed;
		}
	}
}