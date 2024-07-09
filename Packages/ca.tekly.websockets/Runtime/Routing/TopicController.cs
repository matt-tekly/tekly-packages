using System;
using System.Collections.Generic;
using Tekly.WebSockets.Core;

namespace Tekly.WebSockets.Routing
{
	public interface ITopicController : IDisposable
	{
		void ClientAdded(Client client, string topicId);
		void ClientRemoved(Client client);

		void ReceivedSend(Client client, TopicFrame frame);

		event Action<object> Emit;
	}

	public abstract class HistoricalTopic<T> : ITopicController
	{
		public event Action<object> Emit;
		private readonly List<T> m_history = new List<T>();

		public virtual void Dispose() { }

		public void ClientAdded(Client client, string topicId)
		{
			foreach (var message in m_history) {
				client.SendJson(topicId, message);
			}
		}

		public void ClientRemoved(Client client) { }
		public void ReceivedSend(Client client, TopicFrame frame) { }

		protected void Send(T message)
		{
			m_history.Add(message);
			Emit?.Invoke(message);
		}
	}

	public abstract class ValueTopic<T> : ITopicController where T : class
	{
		public event Action<object> Emit;

		private T m_value;

		public virtual void Dispose() { }

		public void ClientAdded(Client client, string topicId)
		{
			client.SendJson(topicId, m_value);
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
			Emit?.Invoke(message);
		}

		protected abstract T GetValue();
	}
}