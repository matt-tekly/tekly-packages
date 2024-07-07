using System;

namespace Tekly.WebSockets.Core
{
	public class WebSocketRequest
	{
		public string Method { get; set; }
		public string Path { get; set; }
		public string HttpVersion { get; set; }
		public string SecurityKey { get; set; }
		public string SecurityVersion { get; set; }
		public string Upgrade { get; set; }
		
		public bool IsValid =>
			Method == "GET"
			&& !string.IsNullOrEmpty(SecurityKey)
			&& !string.IsNullOrEmpty(SecurityVersion)
			&& Upgrade == "websocket";

		public WebSocketRequest(string requestData)
		{
			var lines = requestData.Split(new[] { "\r\n" }, StringSplitOptions.None);
			var requestLine = lines[0].Split(' ');
			
			Method = requestLine[0];
			Path = requestLine[1];
			HttpVersion = requestLine[2];
			
			for (var i = 1; i < lines.Length; i++) {
				var line = lines[i];
				if (string.IsNullOrWhiteSpace(line)) {
					break;
				}

				var colonIndex = line.IndexOf(':');
				if (colonIndex > 0) {
					var key = line.Substring(0, colonIndex).Trim();
					var value = line.Substring(colonIndex + 1).Trim();

					if (key == "Sec-WebSocket-Key") {
						SecurityKey = value;
					} else if (key == "Sec-WebSocket-Version"){
						SecurityVersion = value;
					} else if (key == "Upgrade"){
						Upgrade = value;
					}
				}
			}
		}
	}
}