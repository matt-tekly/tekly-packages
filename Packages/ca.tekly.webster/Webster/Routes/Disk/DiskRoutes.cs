//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

#if WEBSTER_ENABLE || UNITY_EDITOR && WEBSTER_ENABLE_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Tekly.Common.LocalFiles;
using Tekly.Webster.Routing;
using Tekly.Webster.Utility;

namespace Tekly.Webster.Routes.Disk
{
	public class DiskRoutes : IRouteHandler
	{
		public bool TryHandleRoute(string route, HttpListenerRequest request, HttpListenerResponse response)
		{
			if (!route.StartsWith("/api/disk")) {
				return false;
			}

			HandleRequest(request, response);
			return true;
		}

		public IEnumerable<RouteDescriptor> GetRouteDescriptors()
		{
			return Enumerable.Empty<RouteDescriptor>();
		}

		private static void HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
		{
			// Remove "/api/disk" to get the actual file path
			var filePath = request.Url.LocalPath.Substring(10);
			var path = LocalFile.GetFullPath(filePath).Replace('\\', '/');

			if (request.HttpMethod == "GET" || request.HttpMethod == "DELETE") {
				if (!DiskEntryExists(path)) {
					response.StatusCode = (int) HttpStatusCode.NotFound;
					return;
				}

				if (request.HttpMethod == "GET") {
					HandleGet(response, path);
				} else if (request.HttpMethod == "DELETE") {
					HandleDelete(response, path);
				}
			}

			if (request.HttpMethod == "PUT") {
				HandlePut(request, response, path);
			}
		}

		private static void HandleGet(HttpListenerResponse response, string path)
		{
			var attr = File.GetAttributes(path);
			if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
				var dr = new DirectoryResponse {
					PersistentDataPath = Path.GetFullPath(LocalFile.Directory),
					Directory = DirectorySummarizer.Summarize(path, Path.GetFullPath(LocalFile.Directory))
				};

				response.WriteJson(dr);
			} else {
				ResponseUtility.SetResponseContent(path, response);
				
				using (var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					response.WriteContent(fileStream);	
				}
			}
		}

		private static void HandleDelete(HttpListenerResponse response, string path)
		{
			var attr = File.GetAttributes(path);
			if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
				Directory.Delete(path, true);
			} else {
				File.Delete(path);
			}

			response.StatusCode = 200;
			response.WriteText("Okay");
		}

		private static void HandlePut(HttpListenerRequest request, HttpListenerResponse response, string path)
		{
			try {
				using (var fs = File.OpenWrite(path)) {
					request.InputStream.CopyTo(fs);
				}

				response.StatusCode = 200;
				response.WriteText("Okay");
			} catch (Exception ex) {
				response.StatusCode = 500;
				response.WriteText("Failed to write: " + ex);
			}
		}

		private static bool DiskEntryExists(string path)
		{
			return File.Exists(path) || Directory.Exists(path);
		}
	}
}
#endif