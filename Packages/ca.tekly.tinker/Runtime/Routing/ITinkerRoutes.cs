using System.Net;

namespace Tekly.Tinker.Routing
{
	public interface ITinkerRoutes
	{
		bool TryHandle(string route, HttpListenerRequest request, HttpListenerResponse response);
	}
}