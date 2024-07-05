using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tekly.WebSockets.Topics
{
	public class Topic
	{
		private readonly string m_id;
		private readonly List<Client> m_clients = new List<Client>();

		public Topic(string id)
		{
			m_id = id;
		}
		
		public void Subscribe(Client client)
		{
			m_clients.Add(client);
		}
		
		public void Unsubscribe(Client client)
		{
			m_clients.Remove(client);
		}

		public void SendJson<T>(T obj)
		{
			var json = JsonConvert.SerializeObject(obj);
			var frame = TopicFrame.EncodeFrame(FrameCommands.SEND, m_id, "json", json);
			
			foreach (var client in m_clients) {
				client.Send(frame);	
			}
		}
	}
}