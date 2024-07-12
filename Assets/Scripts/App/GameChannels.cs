using System;
using Tekly.Common.LifeCycles;
using Tekly.Logging;
using Tekly.Tinker.Core;
using Tekly.WebSockets.Channeling;

namespace TeklySample.App
{
	public class GameChannels
	{
		private IDisposable m_logChannel;
		private IDisposable m_logStatsChannel;
		
		public GameChannels()
		{
			LifeCycle.Instance.Quit += Quit;
			
			var logChannel = TinkerServer.Instance.Channels.GetChannel("logs");
			m_logChannel = new LogChannel(logChannel);
			
			var logStatsChannel = TinkerServer.Instance.Channels.GetChannel("logs/stats");
			m_logStatsChannel = new LogStatsChannel(logStatsChannel);
		}

		private void Quit()
		{
			m_logChannel.Dispose();
			m_logStatsChannel.Dispose();
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