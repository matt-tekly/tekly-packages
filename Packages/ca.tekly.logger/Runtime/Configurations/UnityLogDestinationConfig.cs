using Tekly.Logging.LogDestinations;

namespace Tekly.Logging.Configurations
{
	public class UnityLogDestinationConfig : LogDestinationConfig
	{
		public LogPrefixes[] Prefixes;
		
		public override ILogDestination CreateInstance()
		{
			return new UnityLogDestination(name, Prefixes);
		}
	}
}