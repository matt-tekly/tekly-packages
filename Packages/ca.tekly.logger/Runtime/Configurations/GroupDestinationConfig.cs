using Tekly.Logging.LogDestinations;
using UnityEngine;

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
}