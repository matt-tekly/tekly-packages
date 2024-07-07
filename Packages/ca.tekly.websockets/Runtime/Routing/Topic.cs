using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Tekly.WebSockets.Core;

namespace Tekly.WebSockets.Routing
{
	public class Topic
	{
		public readonly string Id;
		public bool IsActive => m_clients.Count > 0 || m_controller != null;

		public ITopicController Controller {
			get => m_controller;
			set {
				m_controller = value;
				
				if (m_controller != null) {
					foreach (var client in m_clients) {
						m_controller.ClientAdded(client);
					}
				}
			}
		}
		
		private readonly List<Client> m_clients = new List<Client>();
		private readonly JsonSerializerSettings m_serializerSettings;
		private ITopicController m_controller;
		
		public Topic(string id)
		{
			Id = id;
			m_serializerSettings = new JsonSerializerSettings();
			m_serializerSettings.Converters.Add(new StringEnumConverter());
		}
		
		public void Subscribe(Client client)
		{
			m_clients.Add(client);
			m_controller?.ClientAdded(client);
		}
		
		public void Unsubscribe(Client client)
		{
			m_clients.Remove(client);
			m_controller?.ClientRemoved(client);
		}

		public void SendJson<T>(T obj)
		{
			var json = JsonConvert.SerializeObject(obj, m_serializerSettings);
			var frame = TopicFrame.EncodeFrame(FrameCommands.SEND, Id, "json", json);
			
			foreach (var client in m_clients) {
				client.Send(frame);	
			}
		}

		internal void ProcessSend(Client client, TopicFrame frame)
		{
			m_controller?.ReceivedSend(client, frame);
		}
	}
}