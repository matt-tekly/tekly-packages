using Tekly.Logging;
using Tekly.Tinker.Core;
using Tekly.WebSockets.Core;
using Tekly.WebSockets.Routing;
using UnityEngine;

namespace Tekly.WebSockets
{
	public class WebSocketServerBehaviour : MonoBehaviour
	{
		private LogTopic m_logTopic;
		private LogStatsTopic m_logStatsTopic;

		private void Awake()
		{
			m_logTopic = new LogTopic(TinkerServer.Instance.Topics.Get("logs"));
			m_logStatsTopic = new LogStatsTopic(TinkerServer.Instance.Topics.Get("logs/stats"));
		}

		private void OnDestroy()
		{
			m_logTopic.Dispose();
			m_logStatsTopic.Dispose();
		}
	}

	public class LogTopic : HistoricalTopic<TkLogMessage>
	{
		public LogTopic(Topic topic) : base(topic)
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
		private readonly Topic m_topic;

		public LogStatsTopic(Topic topic) : base(topic)
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