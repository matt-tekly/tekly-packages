using System.Net;
using System.Net.Sockets;

namespace Tekly.Tinker.Http
{
	public static class HttpUtils
	{
		private static readonly IPEndPoint s_defaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);

		public static int GetAvailablePort()
		{
			using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(s_defaultLoopbackEndpoint);
			return ((IPEndPoint)socket.LocalEndPoint).Port;
		}
		
		public static bool IsPortAvailable(int port)
		{
			var isAvailable = true;

			try {
				var listener = new HttpListener();
				listener.Prefixes.Add($"http://*:{port}/");
				listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
				listener.Start();
				listener.Stop();
			}
			catch (SocketException)
			{
				isAvailable = false;
			}

			return isAvailable;
		}

		public static int GetAvailablePort(int defaultPort)
		{
			if (IsPortAvailable(defaultPort)) {
				return defaultPort;
			}

			return GetAvailablePort();
		}
	}
}