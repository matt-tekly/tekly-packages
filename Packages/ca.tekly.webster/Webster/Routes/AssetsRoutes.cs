//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

#if WEBSTER_ENABLE || UNITY_EDITOR && WEBSTER_ENABLE_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Tekly.Webster.Routing;
using Tekly.Webster.Utility;
using UnityEngine;

namespace Tekly.Webster.Routes
{
	public class AssetsRoutes : IRouteHandler
	{
		private const string ID_ROUTE_PREFIX = "/api/textures/id/";
		private const string NAME_ROUTE_PREFIX = "/api/textures/name/";
		private const string SPRITE_ROUTE_PREFIX = "/api/sprites/id/";
		
		public bool TryHandleRoute(string route, HttpListenerRequest request, HttpListenerResponse response)
		{
			if (request.HttpMethod == "GET") {
				if (route.StartsWith(ID_ROUTE_PREFIX)) {
					var instanceId = int.Parse(request.Url.LocalPath.Substring(ID_ROUTE_PREFIX.Length));
					var texture = WebsterServer.Dispatch(() => UnityApi.FindResource<Texture>(instanceId));

					HandleTextureRequest(texture, response);
					return true;
				}

				if (route.StartsWith(NAME_ROUTE_PREFIX)) {
					var name = request.Url.LocalPath.Substring(NAME_ROUTE_PREFIX.Length);
					var texture = WebsterServer.Dispatch(() => UnityApi.FindResource<Texture>(name));
				
					HandleTextureRequest(texture, response);
					return true;
				}
				
				if (route.StartsWith(SPRITE_ROUTE_PREFIX)) {
					HandleSpriteRequest(request, response);
					return true;
				}
			}

			return false;
		}

		public IEnumerable<RouteDescriptor> GetRouteDescriptors()
		{
			return Enumerable.Empty<RouteDescriptor>();
		}
		
		private static void HandleSpriteRequest(HttpListenerRequest request, HttpListenerResponse response)
		{
			var instanceId = int.Parse(request.Url.LocalPath.Substring(SPRITE_ROUTE_PREFIX.Length));

			var bytes = WebsterServer.Dispatch(() => {
				var sprite = UnityApi.FindResource<Sprite>(instanceId);
				var readableTexture = GetReadableTexture(sprite.texture);

				var textureRect = sprite.textureRect;
				var croppedTexture = new Texture2D(Mathf.RoundToInt(textureRect.width), Mathf.RoundToInt(textureRect.height));

				var pixels = readableTexture.GetPixels(
					Mathf.RoundToInt(textureRect.x),
					Mathf.RoundToInt(textureRect.y),
					Mathf.RoundToInt(textureRect.width),
					Mathf.RoundToInt(textureRect.height)
				);

				croppedTexture.SetPixels(pixels);
				croppedTexture.Apply();

				Object.DestroyImmediate(readableTexture);

				var textureBytes = croppedTexture.EncodeToPNG();

				Object.DestroyImmediate(croppedTexture);

				return textureBytes;
			});

			WritePngBytes(bytes, response);
		}

		private static void HandleTextureRequest(Texture texture, HttpListenerResponse res)
		{
			if (ReferenceEquals(texture, null)) {
				res.StatusCode = (int) HttpStatusCode.NotFound;
				return;
			}

			var bytes = WebsterServer.Dispatch(() => {
				if (texture.isReadable && texture is Texture2D texture2D) {
					return texture2D.EncodeToPNG();
				}
				
				var readableTexture = GetReadableTexture(texture);
				var textureBytes = readableTexture.EncodeToPNG();

				Object.DestroyImmediate(readableTexture);
				return textureBytes;
			});

			WritePngBytes(bytes, res);
		}
		
		private static void WritePngBytes(byte[] bytes, HttpListenerResponse res)
		{
			res.ContentType = "image/png";
			res.ContentEncoding = Encoding.Default;
			
			res.WriteContent(bytes);
		}

		private static Texture2D GetReadableTexture(Texture texture)
		{
			var tmp = RenderTexture.GetTemporary( 
				texture.width,
				texture.height,
				0,
				RenderTextureFormat.Default,
				RenderTextureReadWrite.Linear);

			Graphics.Blit(texture, tmp);
			
			var previous = RenderTexture.active;
			
			RenderTexture.active = tmp;
			
			var myTexture2D = new Texture2D(texture.width, texture.height);
			myTexture2D.name = "TEMP_TEKLY_TEX";
			
			// TODO: SystemInfo.graphicsUVStartsAtTop do we need to do something for this
			// Copy the pixels from the RenderTexture to the new Texture
			myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
			myTexture2D.Apply();
			
			RenderTexture.active = previous;
			
			RenderTexture.ReleaseTemporary(tmp);

			return myTexture2D;
		}
		
	}
}
#endif
