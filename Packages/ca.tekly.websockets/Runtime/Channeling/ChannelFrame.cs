using System.Text;

namespace Tekly.WebSockets.Channeled
{
	public static class FrameCommands
	{
		public const string SEND = "SEND";
		public const string SUBSCRIBE = "SUBSCRIBE";
		public const string UNSUBSCRIBE = "UNSUBSCRIBE";
		
		public const string MESSAGE = "MESSAGE";
	}
	
	public readonly struct ChannelFrameEvt
	{
		public readonly string Command;
		public readonly string ContentType;
		public readonly string Content;

		public ChannelFrameEvt(string command, string contentType, string content)
		{
			Command = command;
			ContentType = contentType;
			Content = content;
		}
	}
	
	public class ChannelFrame
	{
		public string Channel;
		public string Command { get; set; }
		public string Session { get; set; }
		public string ContentType { get; set; }
		public string Content { get; set; }
	}
	
	public class FrameEncoding
	{
		public const string NEWLINE = "\r\n";

		private readonly StringBuilder m_stringBuilder = new StringBuilder();
		
		public string Encode(string command, string session, string channel, string contentType, string content)
		{
			m_stringBuilder.Clear();
			
			m_stringBuilder.Append(command);
			m_stringBuilder.Append(NEWLINE);
			
			m_stringBuilder.Append("Session:");
			m_stringBuilder.Append(session);
			m_stringBuilder.Append(NEWLINE);	
			
			m_stringBuilder.Append("Channel:");
			m_stringBuilder.Append(channel);
			m_stringBuilder.Append(NEWLINE);

			if (contentType != null) {
				m_stringBuilder.Append("ContentType:");
				m_stringBuilder.Append(contentType);
				m_stringBuilder.Append(NEWLINE);
				m_stringBuilder.Append(NEWLINE);

				m_stringBuilder.Append(content);	
			}
			
			return m_stringBuilder.ToString();
		}

		public ChannelFrame Decode(string data)
		{
			var frame = new ChannelFrame();
			
			var split = data.Split("\r\n\r\n");
			var headers = split[0];
			
			var headerPairs = headers.Trim().Split(NEWLINE);
			frame.Command = headerPairs[0];

			for (var i = 1; i < headerPairs.Length; i++) {
				var headerSplit = headerPairs[i].Split(':');
				
				if (headerSplit[0] == "Session") {
					frame.Session = headerSplit[1];
				} else if (headerSplit[0] == "Channel") {
					frame.Channel = headerSplit[1];
				} else if (headerSplit[0] == "ContentType") {
					frame.ContentType = headerSplit[1];
				}
			}

			if (split.Length > 1) {
				frame.Content = split[1];
			}

			return frame;
		}
	}
}