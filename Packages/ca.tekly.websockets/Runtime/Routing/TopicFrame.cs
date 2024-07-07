using System;
using System.Collections.Generic;
using System.Text;

namespace Tekly.WebSockets.Routing
{

	public static class FrameCommands
	{
		public const string SEND = "SEND";
		public const string SUBSCRIBE = "SUBSCRIBE";
		public const string UNSUBSCRIBE = "UNSUBSCRIBE";
	}
	
	public class TopicFrame
	{
		public const string NEWLINE = "\r\n";

		public readonly string Command;
		public readonly string Content;

		public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();

		private const int SEARCH_LENGTH = 4;
		private static readonly byte[] s_searchSequence = Encoding.UTF8.GetBytes(NEWLINE + NEWLINE);

		public TopicFrame(string data)
		{
			var split = data.Split("\r\n\r\n");
			var headers = split[0];
			
			var headerPairs = headers.Trim().Split(NEWLINE);
			Command = headerPairs[0];

			for (var i = 1; i < headerPairs.Length; i++) {
				var headerSplit = headerPairs[i].Split(':');
				Headers[headerSplit[0]] = headerSplit[1];
			}

			if (split.Length > 1) {
				Content = split[1];
			}
		}

		public TopicFrame(byte[] bytes)
		{
			var (result, endIndex) = ReadUntilDoubleNewline(bytes);

			if (endIndex != -1) {
				string header = Encoding.UTF8.GetString(result);

				var lines = header.Split(new[] { "\r\n" }, StringSplitOptions.None);
				Command = lines[0];

				for (var i = 1; i < lines.Length; i++) {
					var line = lines[i];
					if (string.IsNullOrWhiteSpace(line)) {
						break;
					}

					var colonIndex = line.IndexOf(':');
					if (colonIndex > 0) {
						var key = line.Substring(0, colonIndex).Trim();
						var value = line.Substring(colonIndex + 1).Trim();
						Headers[key] = value;
					}
				}

				if (endIndex < bytes.Length) {
					Content = Encoding.UTF8.GetString(bytes, endIndex, bytes.Length - endIndex);
				}
			} else {
				throw new Exception("Failed to parse TopicMessage: Double newline sequence not found.");
			}
		}

		public static (byte[] result, int endIndex) ReadUntilDoubleNewline(byte[] buffer)
		{
			for (var i = 0; i <= buffer.Length - SEARCH_LENGTH; i++) {
				var found = true;
				for (var j = 0; j < SEARCH_LENGTH; j++) {
					if (buffer[i + j] != s_searchSequence[j]) {
						found = false;
						break;
					}
				}

				if (found) {
					var result = new byte[i + SEARCH_LENGTH];
					Array.Copy(buffer, result, i + SEARCH_LENGTH);
					return (result, i + SEARCH_LENGTH);
				}
			}

			// If the sequence is not found, return the entire buffer
			return (buffer, -1);
		}
		
		public static string EncodeFrame(string command, string topic)
		{
			var sb = new StringBuilder();
			sb.Append(command);
			sb.Append(NEWLINE);

			sb.Append("Topic:");
			sb.Append(topic);
			sb.Append(NEWLINE);
			
			return sb.ToString();
		}
		
		public static string EncodeFrame(string command, string topic, string contentType, string content)
		{
			var sb = new StringBuilder();
			sb.Append(command);
			sb.Append(NEWLINE);

			sb.Append("Topic:");
			sb.Append(topic);
			sb.Append(NEWLINE);
			
			sb.Append("Content-Type:");
			sb.Append(contentType);
			sb.Append(NEWLINE);
			sb.Append(NEWLINE);

			sb.Append(content);
			
			return sb.ToString();
		}
	}
}