using System;
using System.Collections.Generic;
using Tekly.WebSockets.Core;

namespace Tekly.WebSockets.Routing
{
	public interface ITopicController : IDisposable
	{
		void ClientAdded(Client client);
		void ClientRemoved(Client client);

		void ReceivedSend(Client client, TopicFrame frame);
	}

	public abstract class HistoricalTopic<T> : ITopicController
	{
		private readonly Topic m_topic;
		private readonly List<T> m_history = new List<T>();

		protected HistoricalTopic(Topic topic)
		{
			m_topic = topic;
			m_topic.Controller = this;
		}

		public virtual void Dispose()
		{
			m_topic.Controller = null;
		}

		public void ClientAdded(Client client)
		{
			foreach (var message in m_history) {
				client.SendJson(m_topic.Id, message);
			}
		}

		public void ClientRemoved(Client client) { }
		public void ReceivedSend(Client client, TopicFrame frame) { }

		protected void Send(T message)
		{
			m_history.Add(message);
			m_topic.SendJson(message);
		}
	}

	public abstract class ValueTopic<T> : ITopicController where T : class
	{
		private readonly Topic m_topic;

		private T m_value;

		protected ValueTopic(Topic topic)
		{
			m_topic = topic;
			m_topic.Controller = this;
		}

		public virtual void Dispose()
		{
			m_topic.Controller = null;
		}

		public void ClientAdded(Client client)
		{
			client.SendJson(m_topic.Id, m_value);
		}

		public void ClientRemoved(Client client) { }
		public void ReceivedSend(Client client, TopicFrame frame) { }

		protected void UpdateValue()
		{
			Send(GetValue());
		}

		private void Send(T message)
		{
			m_value = message;
			m_topic.SendJson(message);
		}

		protected abstract T GetValue();
	}
}