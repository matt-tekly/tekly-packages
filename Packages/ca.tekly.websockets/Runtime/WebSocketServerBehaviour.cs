using System;
using UnityEngine;

namespace Tekly.WebSockets
{
	public class TestData
	{
		public int Number;
		public string Time;
	}
	
	public class WebSocketServerBehaviour : MonoBehaviour
	{
		[SerializeField] private int m_port;

		private WebSocketServer m_server;
		private Topics.Topics m_topics;

		private void Awake()
		{
			m_server = new WebSocketServer(m_port);
			m_server.Start();

			m_topics = new Topics.Topics(m_server.Clients);
		}

		private void OnDestroy()
		{
			if (m_server != null) {
				m_server.Stop();
			}
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.P)) {
				var testData = new TestData();
				testData.Number = Time.frameCount;
				testData.Time = DateTime.Now.ToString();
			
				m_topics.SendJson(Topics.Topics.GENERAL, testData);
			}
		}
	}
}