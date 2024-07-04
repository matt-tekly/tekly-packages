using UnityEngine;

namespace Tekly.WebSockets
{
	public class WebSocketServerBehaviour : MonoBehaviour
	{
		[SerializeField] private int m_port;

		private WebSocketServer m_server;

		private void Awake()
		{
			m_server = new WebSocketServer(m_port);
			m_server.Start();
		}

		private void OnDestroy()
		{
			if (m_server != null) {
				m_server.Stop();
			}
		}
	}
}