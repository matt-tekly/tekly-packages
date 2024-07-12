using System;

namespace Tekly.WebSockets.Channeled
{
	public class Channel
	{
		public readonly string Id;

		public event Action<ChannelFrameEvt> Messaged;
		public event Action<ChannelFrameEvt> Received;
		public event Action<Subscription> Subscribed;
		
		private readonly Channels m_channels;

		public Channel(string id, Channels channels)
		{
			Id = id;
			m_channels = channels;
		}

		public void Message<T>(T obj)
		{
			var json = m_channels.Serialize(obj);
			Messaged?.Invoke(new ChannelFrameEvt(FrameCommands.MESSAGE, "json", json));
		}
		
		public void Receive(string contentType, string content)
		{
			Received?.Invoke(new ChannelFrameEvt(FrameCommands.SEND, contentType, content));
		}

		public void Subscribe(Subscription subscription)
		{
			Subscribed?.Invoke(subscription);
		}
	}
}