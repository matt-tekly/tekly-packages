using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Tekly.WebSockets.Core;
using UnityEngine;

namespace Tekly.WebSockets.Channeling
{
	public class Channels : IDisposable
	{
		private readonly Clients m_clients;
		private readonly FrameEncoding m_frameEncoding;

		private readonly Dictionary<Client, ClientConnection> m_clientConnections = new Dictionary<Client, ClientConnection>();
		private readonly Dictionary<string, Channel> m_channels = new Dictionary<string, Channel>();
		private readonly JsonSerializerSettings m_serializerSettings;

		public Channels(Clients clients)
		{
			m_clients = clients;
			
			m_clients.ClientConnected += OnClientConnected;
			m_clients.ClientClosed += OnClientClosed;

			m_serializerSettings = new JsonSerializerSettings();
			m_serializerSettings.Converters.Add(new StringEnumConverter());
		}

		public void Dispose()
		{
			m_clients.ClientConnected -= OnClientConnected;
			m_clients.ClientClosed -= OnClientClosed;
		}

		public Channel GetChannel(string channelId)
		{
			if (!m_channels.TryGetValue(channelId, out var channel)) {
				channel = new Channel(channelId, this);
				m_channels[channelId] = channel;
			}

			return channel;
		}

		public string Serialize<T>(T obj)
		{
			return JsonConvert.SerializeObject(obj, m_serializerSettings);
		}
		
		public T Deserialize<T>(string json)
		{
			return JsonConvert.DeserializeObject<T>(json, m_serializerSettings);
		}

		private void OnClientConnected(Client client)
		{
			if (!m_clientConnections.TryGetValue(client, out var clientConnection)) {
				clientConnection = new ClientConnection(client, this);
				m_clientConnections[client] = clientConnection;
			} else {
				Debug.LogError("Client connecting twice");
			}
		}

		private void OnClientClosed(Client client)
		{
			if (!m_clientConnections.Remove(client)) {
				Debug.LogError("Trying to remove client that didn't exist");
			}
		}
	}
}