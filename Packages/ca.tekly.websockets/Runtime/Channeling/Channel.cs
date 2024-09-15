using System;

namespace Tekly.WebSockets.Channeling
{
	public interface IChannel
	{
		event Action<ChannelFrameEvt> Messaged;
		event Action<ChannelFrameEvt> Received;
		event Action<Subscription> Subscribed;
		string Id { get; }

		void Message<T>(T obj);
		void Receive(string contentType, string content);
		void Subscribe(Subscription subscription);
	}

	public class Channel : IChannel
	{
		public  string Id { get; }

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
	
	public class ChannelStub : IChannel
	{
		#pragma warning disable CS0067
		public event Action<ChannelFrameEvt> Messaged;
		public event Action<ChannelFrameEvt> Received;
		public event Action<Subscription> Subscribed;
		#pragma warning restore CS0067
		
		public string Id { get; }
		public void Message<T>(T obj) { }
		public void Receive(string contentType, string content) { }
		public void Subscribe(Subscription subscription) { }
	}
}