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
			if (Application.isPlaying) {
				TkLogger.Initialize(this);	
			}
		}
#endif
	}

	[Serializable]
	public class LoggerConfig
	{
		[Tooltip("A namespace or fully full class name. \"Namespace.ChildNameSpace\" would apply to anything under that namespace")]
		public string Logger;
		
		[Tooltip("The minimum log level to be logged")]
		public TkLogLevel Level;
		
		[Tooltip("An optional destination to use instead of the default")]
		public LogDestinationConfig DestinationOverride;
	}
}