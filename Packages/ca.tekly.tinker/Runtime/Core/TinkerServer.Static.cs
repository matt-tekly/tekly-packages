#if UNITY_EDITOR && TINKER_ENABLED_EDITOR
#define TINKER_ENABLED
#endif

using Tekly.Tinker.Routing;
using Tekly.WebSockets.Channeling;

namespace Tekly.Tinker.Core
{
	public partial class TinkerServer
	{
#if TINKER_ENABLED
		public static void Initialize(int port = PORT_DEFAULT)
		{
			Instance.InitializeInternal(port);
		}

		public static ITinkerRoutes AddClassHandler(object handler)
		{
			return Instance.AddClassHandlerInternal(handler);
		}

		public static ITinkerRoutes AddHandler<T>() where T : new()
		{
			return Instance.AddHandlerInternal<T>();
		}

		public static void RemoveHandler(ITinkerRoutes routes)
		{
			Instance.RemoveHandlerInternal(routes);
		}

		public static IChannel GetChannel(string channelName)
		{
			return Instance.Channels.GetChannel(channelName);
		}
		
		public static TinkerHome AddHomeCard(string name, string url, int width, int height, bool isDefault = false, bool noResize = false)
		{
			return Instance.Home.Add(name, url, width, height, isDefault, noResize);
		}
#else
		private static TinkerHome s_tinkerHome = new TinkerHome();

		public static void Initialize(int port = 0) { }

		public static ITinkerRoutes AddClassHandler(object handler)
		{
			return new TinkerRoutesStub();
		}

		public static ITinkerRoutes AddHandler<T>() where T : new()
		{
			return new TinkerRoutesStub();
		}

		public static void RemoveHandler(ITinkerRoutes routes) { }

		public static IChannel GetChannel(string channelName)
		{
			return new ChannelStub();
		}
		
		public static TinkerHome AddHomeCard(string name, string url, int width, int height, bool isDefault = false, bool noResize = false)
		{
			return s_tinkerHome.Add(name, url, width, height, isDefault, noResize);
		}
#endif
	}
}