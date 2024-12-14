using Tekly.Logging.LogDestinations;
using UnityEngine;

namespace Tekly.Logging.Configurations
{
	[CreateAssetMenu(menuName = "Tekly/Logger/Unity Destination", fileName = "UnityLogDestinationConfig", order = 0)]
	public class UnityLogDestinationConfig : LogDestinationConfig
	{
		public LogPrefixes[] Prefixes;
		
		public override ILogDestination CreateInstance()
		{
			return new UnityLogDestination(name, Prefixes);
		}
	}
}