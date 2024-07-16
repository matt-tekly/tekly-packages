using System.Linq;
using Tekly.Logging.Configurations;
using UnityEngine;

namespace Tekly.Logging.LogDestinations
{
	public class GroupDestination : ILogDestination
	{
		public string Name { get; }

		private readonly ILogDestination[] m_destinations;

		public GroupDestination(GroupDestinationConfig groupDestination)
		{
			Name = groupDestination.name;
			m_destinations = groupDestination.Destinations
				.Select(TkLogger.GetDestination)
				.ToArray();
		}

		public void Dispose() { }

		public void LogMessage(TkLogMessage message, LogSource logSource)
		{
			for (var index = 0; index < m_destinations.Length; index++) {
				var logDestination = m_destinations[index];
				logDestination.LogMessage(message, logSource);
			}
		}

		public void LogMessage(TkLogMessage message, Object context, LogSource logSource)
		{
			for (var index = 0; index < m_destinations.Length; index++) {
				var logDestination = m_destinations[index];
				logDestination.LogMessage(message, context, logSource);
			}
		}

		public void Update() { }
	}
}