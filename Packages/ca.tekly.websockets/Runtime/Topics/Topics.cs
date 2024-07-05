using System.Collections.Generic;

namespace Tekly.WebSockets.Topics
{
	public class Topics
	{
		private readonly Clients m_clients;
		
		private Dictionary<string, Topic> m_topics = new Dictionary<string, Topic>();

		public Topics(Clients clients)
		{
			m_clients = clients;

			m_clients.ClientConnected += OnClientConnected;
			m_clients.ClientClosed += OnClientClosed;
		}

		private void OnClientConnected(Client client)
		{
			client.ReceivedText += ClientOnReceivedText;
		}
		
		private void OnClientClosed(Client client)
		{
			client.ReceivedText -= ClientOnReceivedText;
		}

		private void ClientOnReceivedText(Client arg1, string arg2)
		{
			
		}

		public void SendJson<T>(string topicId, T message)
		{
			if (m_topics.TryGetValue(topicId, out var topic)) {
				topic.SendJson<T>(message);
			}
		}
	}
}