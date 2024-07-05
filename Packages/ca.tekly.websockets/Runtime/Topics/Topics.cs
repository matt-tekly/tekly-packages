using System.Collections.Generic;
using UnityEngine;

namespace Tekly.WebSockets.Topics
{
	public class Topics
	{
		private readonly Clients m_clients;
		
		private Dictionary<string, Topic> m_topics = new Dictionary<string, Topic>();

		public const string GENERAL = "general";

		public Topics(Clients clients)
		{
			m_clients = clients;

			m_clients.ClientConnected += OnClientConnected;
			m_clients.ClientClosed += OnClientClosed;
		}

		private void OnClientConnected(Client client)
		{
			client.ReceivedText += ClientOnReceivedText;
			Subscribe(client, GENERAL);
		}
		
		private void OnClientClosed(Client client)
		{
			client.ReceivedText -= ClientOnReceivedText;
			Unsubscribe(client, GENERAL);
		}

		private void ClientOnReceivedText(Client arg1, string text)
		{
			Debug.Log("Topics Received Text: " + text);
		}

		public void SendJson<T>(string topicId, T message)
		{
			if (m_topics.TryGetValue(topicId, out var topic)) {
				topic.SendJson(message);
			}
		}

		private void Subscribe(Client client, string topicId)
		{
			if (!m_topics.TryGetValue(topicId, out var topic)) {
				topic = new Topic(topicId);
				m_topics[topicId] = topic;
			}
			
			topic.Subscribe(client);
		}
		
		private void Unsubscribe(Client client, string topicId)
		{
			if (m_topics.TryGetValue(topicId, out var topic)) {
				topic.Unsubscribe(client);
			}
			
			// TODO: Should this log an error if the topic wasn't found?
		}
	}
}