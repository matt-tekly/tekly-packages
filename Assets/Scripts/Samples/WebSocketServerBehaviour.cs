using System;
using Tekly.Logging;
using Tekly.Tinker.Core;
using Tekly.WebSockets.Channeled;
using UnityEngine;

namespace Tekly.WebSockets
{
	public class WebSocketServerBehaviour : MonoBehaviour
	{
		private IDisposable m_logChannel;
		private IDisposable m_logStatsChannel;

		private void Awake()
		{
			var logChannel = TinkerServer.Instance.Channels.GetChannel("logs");
			m_logChannel = new LogChannel(logChannel);
			
			var logStatsChannel = TinkerServer.Instance.Channels.GetChannel("logs/stats");
			m_logStatsChannel = new LogStatsChannel(logStatsChannel);
		}

		private void OnDestroy()
		{
			m_logChannel.Dispose();
			m_logStatsChannel.Dispose();
		}

		private void OnApplicationQuit()
		{
			OnDestroy();
		}
	}

	public class LogChannel : HistoricalChannel<TkLogMessage>
	{
		public LogChannel(Channel channel) : base(channel)
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

	public class LogStatsChannel : ValueChannel<DataList>
	{
		public LogStatsChannel(Channel channel) : base(channel)
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