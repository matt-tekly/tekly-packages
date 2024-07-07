using System.Net;
using Tekly.Common.Utils;
using Tekly.Tinker.Core;
using Tekly.Tinker.Http;
using Tekly.Tinker.Routing;

namespace Tekly.Tinker.Assets
{
	public class TextureRoutes : ITinkerRoutes
	{
		private const string ID_ROUTE_PREFIX = "/api/textures/id/";
		private const string NAME_ROUTE_PREFIX = "/api/textures/name/";
		private const string SPRITE_ROUTE_PREFIX = "/api/sprites/id/";
		
		public bool TryHandle(string route, HttpListenerRequest request, HttpListenerResponse response)
		{
			if (request.HttpMethod == "GET") {
				if (route.StartsWith(ID_ROUTE_PREFIX)) {
					var instanceId = int.Parse(request.Url.LocalPath.Substring(ID_ROUTE_PREFIX.Length));
					response.WritePng(TextureUtils.GetTextureBytes(instanceId));
					return true;
				}

				if (route.StartsWith(NAME_ROUTE_PREFIX)) {
					var name = request.Url.LocalPath.Substring(NAME_ROUTE_PREFIX.Length);
					response.WritePng(TextureUtils.GetTextureBytes(name));
					return true;
				}
				
				if (route.StartsWith(SPRITE_ROUTE_PREFIX)) {
					var instanceId = int.Parse(request.Url.LocalPath.Substring(SPRITE_ROUTE_PREFIX.Length));
					response.WritePng(TextureUtils.GetSpriteBytes(instanceId));
					return true;
				}
			}

			return false;
		}
	}
}