using Tekly.Logging.LogDestinations;
using UnityEngine;

namespace Tekly.Logging.Configurations
{
	[CreateAssetMenu(menuName = "Tekly/Logger/Unity Destination", fileName = "UnityLogDestinationConfig", order = 0)]
	public class UnityLogDestinationConfig : LogDestinationConfig
	{
		public LogPrefixes[] Prefixes;
		public string TimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
		public string Separator = "";
		public bool UseUtc;
		public string MessageSeparator = " ";
		
		public override ILogDestination CreateInstance()
		{
			return new UnityLogDestination(name, this);
		}
	}
}