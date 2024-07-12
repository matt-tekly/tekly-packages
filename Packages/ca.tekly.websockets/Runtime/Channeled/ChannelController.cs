using System;
using System.Collections.Generic;

namespace Tekly.WebSockets.Channeled
{
	public class ChannelController : IDisposable
	{
		protected readonly Channel m_channel;

		public ChannelController(Channel channel)
		{
			m_channel = channel;
			m_channel.Received += ReceivedFrame;
			m_channel.Subscribed += ClientSubscribed;
		}

		public virtual void Dispose()
		{
			m_channel.Received -= ReceivedFrame;
			m_channel.Subscribed -= ClientSubscribed;
		}

		protected virtual void ReceivedFrame(ChannelFrameEvt evt) { }

		protected virtual void ClientSubscribed(Subscription subscription) { }
	}

	public abstract class HistoricalChannel<T> : ChannelController
	{
		private readonly List<T> m_history = new List<T>();

		protected HistoricalChannel(Channel channel) : base(channel) { }

		protected void Message(T value)
		{
			m_history.Add(value);
			m_channel.Message(value);
		}

		protected override void ClientSubscribed(Subscription subscription)
		{
			foreach (var value in m_history) {
				subscription.Message(value);
			}
		}
	}

	public abstract class ValueChannel<T> : ChannelController
	{
		private T m_value;

		protected ValueChannel(Channel channel) : base(channel) { }

		protected void Message(T value)
		{
			m_value = value;
			m_channel.Message(value);
		}

		protected override void ClientSubscribed(Subscription subscription)
		{
			if (m_value != null) {
				subscription.Message(m_value);	
			}
		}
	}
}