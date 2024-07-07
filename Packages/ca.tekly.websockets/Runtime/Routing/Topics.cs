using System.Collections.Generic;
using Tekly.WebSockets.Core;

namespace Tekly.WebSockets.Routing
{
	public class Topics
	{
		private readonly Clients m_clients;
		private readonly Dictionary<string, Topic> m_topics = new Dictionary<string, Topic>();
		
		public Topics(Clients clients)
		{
			m_clients = clients;

			m_clients.ClientConnected += OnClientConnected;
			m_clients.ClientClosed += OnClientClosed;
		}

		public Topic Get(string topicId)
		{
			if (!m_topics.TryGetValue(topicId, out var topic)) {
				topic = new Topic(topicId);
				m_topics[topicId] = topic;
			}

			return topic;
		}

		private void OnClientConnected(Client client)
		{
			client.ReceivedText += ProcessPayload;
		}
		
		private void OnClientClosed(Client client)
		{
			var closedTopics = new List<Topic>();
			
			client.ReceivedText -= ProcessPayload;
			foreach (var topic in m_topics.Values) {
				topic.Unsubscribe(client);
				
				if (!topic.IsActive) {
					closedTopics.Add(topic);
				}
			}

			foreach (var closedTopic in closedTopics) {
				m_topics.Remove(closedTopic.Id);
			}
		}

		private void ProcessPayload(Client client, string text)
		{
			var frame = new TopicFrame(text);
			
			if (frame.Command == FrameCommands.SUBSCRIBE) {
				Subscribe(client, frame.Headers["Topic"]);
			}
			
			if (frame.Command == FrameCommands.UNSUBSCRIBE) {
				Unsubscribe(client, frame.Headers["Topic"]);
			}

			if (frame.Command == FrameCommands.SEND) {
				ProcessSend(client, frame);
			}
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
				if (!topic.IsActive) {
					m_topics.Remove(topicId);
				}
			}
		}

		private void ProcessSend(Client client, TopicFrame frame)
		{
			var topicId = frame.Headers["Topic"];
			if (m_topics.TryGetValue(topicId, out var topic)) {
				topic.ProcessSend(client, frame);
			}
		}
	}
}