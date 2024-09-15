using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Tekly.Tinker.Http
{
	public static class HttpExtensions
	{
		public static void WriteHtml(this HttpListenerResponse response, string content)
		{
			response.WriteText(content, "html");
		}
		
		public static void WriteHtml(this HttpListenerResponse response, HtmlContent content)
		{
			response.WriteText(content.Content, "html");
		}

		public static void WriteCss(this HttpListenerResponse response, string content)
		{
			response.WriteText(content, "css");
		}
		
		public static void WriteJavascript(this HttpListenerResponse response, string content)
		{
			response.WriteText(content, "javascript");
		}

		public static void WriteText(this HttpListenerResponse response, string content)
		{
			response.WriteText(content, "text");
		}

		public static void WriteText(this HttpListenerResponse response, string content, string type)
		{
			response.ContentType = $"text/{type}";
			response.ContentEncoding = Encoding.UTF8;

			response.WriteContent(Encoding.UTF8.GetBytes(content));
		}
		
		public static void WriteJson(this HttpListenerResponse response, string json)
		{
			response.ContentType = "application/json";
			response.ContentEncoding = Encoding.UTF8;

			response.WriteContent(Encoding.UTF8.GetBytes(json));
		}
		
		public static void WritePng(this HttpListenerResponse response, byte[] bytes)
		{
			if (bytes == null) {
				response.StatusCode = (int) HttpStatusCode.NotFound;
				return;
			}
			
			response.ContentType = "image/png";
			response.ContentEncoding = Encoding.Default;
			
			response.WriteContent(bytes);
		}
		
		public static void WritePng(this HttpListenerResponse response, Texture texture)
		{
			WritePng(response, TextureUtils.GetTextureBytes(texture));
		}

		public static void WriteContent(this HttpListenerResponse response, byte[] content)
		{
			if (response == null) {
				throw new ArgumentNullException(nameof(response));
			}

			if (content == null) {
				throw new ArgumentNullException(nameof(content));
			}

			var length = content.Length;

			if (length > 0) {
				response.ContentLength64 = length;
				var outputStream = response.OutputStream;
				outputStream.Write(content, 0, length);
				outputStream.Close();
			} else {
				response.OutputStream.Close();
			}
		}

		public static string ReadBody(this HttpListenerRequest request)
		{
			using var reader = new StreamReader(request.InputStream);
			return reader.ReadToEnd();
		}

		public static NameValueCollection GetQueryParams(this HttpListenerRequest request)
		{
			var collection = new NameValueCollection();
			collection.Add(request.QueryString);

			if (!request.HasEntityBody) {
				return collection;
			}

			var contentType = request.ContentType;
			if (contentType != null && (contentType.StartsWith("application/x-www-form-urlencoded") ||
			                            contentType.StartsWith("multipart/form-data"))) {
				using var body = request.InputStream;
				using var reader = new StreamReader(body, request.ContentEncoding);

				var formData = reader.ReadToEnd();
				ParseQueryString(formData, request.ContentEncoding, collection);
			}

			return collection;
		}
		
		private static void ParseQueryString(string query, Encoding encoding, NameValueCollection result)
		{
			if (string.IsNullOrEmpty(query)) {
				return;
			}

			var decodedLength = query.Length;
			var namePos = 0;
			var first = true;

			while (namePos <= decodedLength) {
				int valuePos = -1, valueEnd = -1;
				for (var q = namePos; q < decodedLength; q++) {
					if ((valuePos == -1) && (query[q] == '=')) {
						valuePos = q + 1;
					} else if (query[q] == '&') {
						valueEnd = q;
						break;
					}
				}

				if (first) {
					first = false;
					if (query[namePos] == '?') {
						namePos++;
					}
				}

				string name;

				if (valuePos == -1) {
					name = null;
					valuePos = namePos;
				} else {
					name = UnityWebRequest.UnEscapeURL(query.Substring(namePos, valuePos - namePos - 1), encoding);
				}

				if (valueEnd < 0) {
					namePos = -1;
					valueEnd = query.Length;
				} else {
					namePos = valueEnd + 1;
				}

				var value = UnityWebRequest.UnEscapeURL(query.Substring(valuePos, valueEnd - valuePos), encoding);

				result.Add(name, value);
				if (namePos == -1) {
					break;
				}
			}
		}
	}
}