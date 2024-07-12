using System;
using Tekly.Logging;
using Tekly.Tinker.Core;
using Tekly.WebSockets.Channeled;
using UnityEngine;

namespace Tekly.WebSockets
{
	public class WebSocketServerBehaviour : MonoBehaviour
	{
		private IDisposable m_logTopic;
		private IDisposable m_logStatsTopic;

		private void Awake()
		{
			var logChannel = TinkerServer.Instance.Channels.GetChannel("logs");
			m_logTopic = new LogTopic(logChannel);
			
			var logStatsChannel = TinkerServer.Instance.Channels.GetChannel("logs/stats");
			m_logStatsTopic = new LogStatsTopic(logStatsChannel);
		}

		private void OnDestroy()
		{
			m_logTopic.Dispose();
			m_logStatsTopic.Dispose();
		}
	}

	public class LogTopic : HistoricalChannel<TkLogMessage>
	{
		public LogTopic(Channel channel) : base(channel)
		{
			TkLogger.MessageLogged += MessageLogged;
		}

		public override void Dispose()
		{
			base.Dispose();
			TkLogger.MessageLogged -= MessageLogged;
		}

		private void MessageLogged(TkLogMessage message)
		{
			Message(message);
		}
	}

	public class LogStatsTopic : ValueChannel<DataList>
	{
		public LogStatsTopic(Channel channel) : base(channel)
		{
			TkLogger.MessageLogged += MessageLogged;
		}

		public override void Dispose()
		{
			base.Dispose();
			TkLogger.MessageLogged -= MessageLogged;
		}

		private void MessageLogged(TkLogMessage message)
		{
			Message(GetValue());
		}
		
		private DataList GetValue()
		{
			return new DataList("Logs")
				.Add("Debug", TkLogger.Stats.Debug)
				.Add("Info", TkLogger.Stats.Info)
				.Add("Warning", TkLogger.Stats.Warning, "yellow")
				.Add("Error", TkLogger.Stats.Error, "red")
				.Add("Exception", TkLogger.Stats.Exception, "red");
		}
	}
}