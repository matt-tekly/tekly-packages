using UnityEngine;

namespace Tekly.Logging.Configurations
{
	public abstract class LogDestinationConfig : ScriptableObject
	{
		public abstract ILogDestination CreateInstance();
	}
}