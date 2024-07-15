using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Logging.Configurations
{
	[CreateAssetMenu(menuName = "Tekly/Logger/Group Destination", fileName = "GroupDestinationConfig", order = 0)]
	public class GroupDestinationConfig : LogDestinationConfig
	{
		public LogDestinationConfig[] Destinations;

		public override ILogDestination CreateInstance()
		{
			return new GroupDestination(this);
		}
	}

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