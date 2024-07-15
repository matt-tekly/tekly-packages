using System;
using UnityEngine;

namespace Tekly.Logging.Configurations
{
	[CreateAssetMenu(menuName = "Tekly/Logger/Profile", fileName = "profile", order = 0)]
	public class LoggingProfileConfig : ScriptableObject
	{
		public TkLogLevel DefaultLevel;
		public LogDestinationConfig DefaultDestination;
		
		public LoggerConfig[] Configs;
		
#if UNITY_EDITOR
		private void OnValidate()
		{
			TkLogger.Initialize(this);
		}
#endif
	}

	[Serializable]
	public class LoggerConfig
	{
		public string Logger;
		public TkLogLevel Level;
		public LogDestinationConfig Destination;
	}
}