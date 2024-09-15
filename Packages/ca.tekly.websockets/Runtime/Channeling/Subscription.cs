using Tekly.WebSockets.Core;

namespace Tekly.WebSockets.Channeling
{
	public class Subscription
	{
		public readonly string SessionId;
		private readonly Channels m_channels;

		private readonly Client m_client;
		private readonly IChannel m_channel;

		private readonly FrameEncoding m_frameEncoding = new FrameEncoding();
		private readonly object m_lock = new object();

		public Subscription(Client client, IChannel channel, string sessionId, Channels channels)
		{
			m_client = client;
			m_channel = channel;
			SessionId = sessionId;
			m_channels = channels;

			m_channel.Messaged += Message;
			m_channel.Subscribe(this);
		}

		public void Close()
		{
			m_channel.Messaged -= Message;
		}

		public void Message(ChannelFrameEvt evt)
		{
			lock (m_lock) {
				var frameData = m_frameEncoding.Encode(evt.Command, SessionId, m_channel.Id, evt.ContentType, evt.Content);
				m_client.Send(frameData);
			}
		}
		
		public void Message<T>(T value)
		{
			var json = m_channels.Serialize(value);
			lock (m_lock) {
				var frameData = m_frameEncoding.Encode(FrameCommands.MESSAGE, SessionId, m_channel.Id, "json", json);
				m_client.Send(frameData);
			}
		}
	}
}