//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.IO;
using System.Net;
using System.Text;

namespace Tekly.Webster.Utility
{
	public static class ResponseUtility
	{
		public static IWebsterSerializer Serializer = new UnityWebsterSerializer();

		public static void SetResponseContent(string path, HttpListenerResponse res)
		{
			if (path.EndsWith(".html")) {
				res.ContentType = "text/html";
				res.ContentEncoding = Encoding.UTF8;
			} else if (path.EndsWith(".js")) {
				res.ContentType = "application/javascript";
				res.ContentEncoding = Encoding.UTF8;
			} else if (path.EndsWith(".css")) {
				res.ContentType = "text/css";
				res.ContentEncoding = Encoding.UTF8;
			} else if (path.EndsWith(".json")) {
				res.ContentType = "application/json";
				res.ContentEncoding = Encoding.UTF8;
			} else if (path.EndsWith(".png")) {
				res.ContentType = "image/png";
				res.ContentEncoding = Encoding.Default;
			} else if (path.EndsWith(".ico")) {
				res.ContentType = "image/x-icon";
				res.ContentEncoding = Encoding.Default;
			} else if (path.EndsWith(".svg")) {
				res.ContentType = "image/svg+xml";
				res.ContentEncoding = Encoding.Default;
			} else if (path.EndsWith(".log")) {
				res.ContentType = "text/text";
				res.ContentEncoding = Encoding.Default;
			}
		}

		public static void WriteText(this HttpListenerResponse response, string content)
		{
			response.ContentType = "text/text";
			response.ContentEncoding = Encoding.UTF8;

			response.WriteContent(Encoding.UTF8.GetBytes(content));
		}

		public static void WriteJson(this HttpListenerResponse response, object obj)
		{
			response.ContentType = "application/json";
			response.ContentEncoding = Encoding.UTF8;

			var content = SerializeObject(obj);
			var bytes = Encoding.UTF8.GetBytes(content);

			response.WriteContent(bytes);
		}

		private static string SerializeObject(object obj)
		{
			if (obj is IToJson toJson) {
				var stringBuilder = new StringBuilder();
				toJson.ToJson(stringBuilder);

				return stringBuilder.ToString();
			}

			return Serializer.Serialize(obj);
		}

		public static void WriteContent(this HttpListenerResponse response, byte[] content)
		{
			if (response == null) {
				throw new ArgumentNullException(nameof(response));
			}
			
			if (content == null) {
				throw new ArgumentNullException(nameof(content));
			}

			long length = content.Length;

			if (length > 0L) {
				response.ContentLength64 = length;
				var outputStream = response.OutputStream;
				if (length <= int.MaxValue) {
					outputStream.Write(content, 0, (int) length);
				} else {
					outputStream.WriteBytes(content, 1024);
				}

				outputStream.Close();
			}
		}

		private static void WriteBytes(this Stream stream, byte[] bytes, int bufferLength)
		{
			using (var source = new MemoryStream(bytes))
				source.CopyTo(stream, bufferLength);
		}
	}
}