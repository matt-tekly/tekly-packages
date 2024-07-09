using System;
using Tekly.Logging;
using Tekly.Tinker.Core;
using Tekly.WebSockets.Routing;
using UnityEngine;

namespace Tekly.WebSockets
{
	public class WebSocketServerBehaviour : MonoBehaviour
	{
		private IDisposable m_logTopic;
		private IDisposable m_logStatsTopic;

		private void Awake()
		{
			m_logTopic = TinkerServer.Instance.Topics.Connect("logs", new LogTopic());
			m_logStatsTopic = TinkerServer.Instance.Topics.Connect("logs/stats", new LogStatsTopic());
		}

		private void OnDestroy()
		{
			m_logTopic.Dispose();
			m_logStatsTopic.Dispose();
		}
	}

	public class LogTopic : HistoricalTopic<TkLogMessage>
	{
		public LogTopic()
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
			Send(message);
		}
	}

	public class LogStatsTopic : ValueTopic<DataList>
	{
		public LogStatsTopic()
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
			UpdateValue();
		}
		
		protected override DataList GetValue()
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