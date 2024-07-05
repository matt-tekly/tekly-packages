using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.UI;

namespace Tekly.WebSockets.Topics
{
	public class Topic
	{
		private List<Client> m_clients = new List<Client>();

		public void OnSubscribe(Client connection)
		{
			// SendJson(new TopicMessage(), connection);
		}

		public void Send(Client connection) { }

		public void Receive(Client connection, WebSocketFrame frame) { }

		public void SendJson<T>(T obj)
		{
			var json = JsonConvert.SerializeObject(obj);

			foreach (var client in m_clients) {
				// TODO: This should have to send a message thingy
				client.Send(json);	
			}
		}
	}
}