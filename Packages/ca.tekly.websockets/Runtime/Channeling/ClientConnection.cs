using System.Collections.Generic;
using System.Linq;
using Tekly.WebSockets.Core;
using UnityEngine;

namespace Tekly.WebSockets.Channeling
{
	public class ClientConnection
	{
		private readonly List<Subscription> m_subscriptions = new List<Subscription>();
		
		private readonly Client m_client;
		private readonly Channels m_channels;
		private readonly FrameEncoding m_frameEncoding = new FrameEncoding();
		
		public ClientConnection(Client client, Channels channels)
		{
			m_client = client;
			m_channels = channels;
			
			client.ReceivedText += ProcessFrameData;
			client.Closed += Close;
		}
		
		private void ProcessFrameData(Client client, string frameData)
		{
			var frame = m_frameEncoding.Decode(frameData);
			
			if (frame.Command == FrameCommands.SUBSCRIBE) {
				Subscribe(frame.Channel, frame.Session);
			}
			
			if (frame.Command == FrameCommands.UNSUBSCRIBE) {
				Unsubscribe(frame.Session);
			}

			if (frame.Command == FrameCommands.SEND) {
				var channel = m_channels.GetChannel(frame.Channel);
				channel.Receive(frame.ContentType, frame.Content);
			}
		}

		private void Subscribe(string channelId, string sessionId)
		{
			var channel = m_channels.GetChannel(channelId);
			var subscription = new Subscription(m_client, channel, sessionId, m_channels);
			m_subscriptions.Add(subscription);
		}
		
		private void Unsubscribe(string sessionId)
		{
			var subscription = m_subscriptions.FirstOrDefault(x => x.SessionId == sessionId);
			
			if (subscription != null) {
				subscription.Close();
				m_subscriptions.Remove(subscription);
			} else {
				Debug.LogError("Unsubscribing from unknown subscription: " + sessionId);
			}
		}

		private void Close(Client client)
		{
			foreach (var subscription in m_subscriptions) {
				subscription.Close();
			}

			m_subscriptions.Clear();
			
			m_client.ReceivedText -= ProcessFrameData;
			m_client.Closed -= Close;
		}
	}
}