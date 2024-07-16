#if UNITY_EDITOR
using System.IO;
using Tekly.Logging.Configurations;
using UnityEditor;
using UnityEngine;

namespace Tekly.Logging
{
	public static class TkLoggerMenus
	{
		private const string DIRECTORY = "Assets/Resources/Logger";
		
		private const string PROFILE_PATH = DIRECTORY + "/default_profile.asset";
		private const string UNITY_LOGGER_PATH = DIRECTORY + "/unity_destination.asset";
		private const string FILE_DESTINATION = DIRECTORY + "/file_destination.asset";
		private const string MAIN_DESTINATION = DIRECTORY + "/main_destination.asset";
		
		[MenuItem("Tools/Tekly/Logger/Create Default Configurations")]
		public static void CreateDefaultConfigurations()
		{
			var profile = ScriptableObject.CreateInstance<LoggingProfileConfig>();
			
			var unityDestination = ScriptableObject.CreateInstance<UnityLogDestinationConfig>();
			unityDestination.Prefixes = new[] { LogPrefixes.Frame, LogPrefixes.Logger };

			var flatFileDestination = ScriptableObject.CreateInstance<FileLogDestinationConfig>();
			flatFileDestination.FileName = "main";
			flatFileDestination.Type = LogFileType.Flat;
			
			var groupDestination = ScriptableObject.CreateInstance<GroupDestinationConfig>();
			groupDestination.Destinations = new LogDestinationConfig[] { unityDestination, flatFileDestination };

			profile.DefaultDestination = groupDestination;
			profile.DefaultLevel = TkLogLevel.Info;

			profile.Configs = new[] {
				new LoggerConfig() {
					Logger = "MyNamespace.Class",
					Level = TkLogLevel.Debug
				}
			};
			
			if (!Directory.Exists(DIRECTORY))
			{
				Directory.CreateDirectory(DIRECTORY);
			}
			
			AssetDatabase.CreateAsset(unityDestination, UNITY_LOGGER_PATH);
			AssetDatabase.CreateAsset(flatFileDestination, FILE_DESTINATION);
			AssetDatabase.CreateAsset(groupDestination, MAIN_DESTINATION);

			AssetDatabase.CreateAsset(profile, PROFILE_PATH);
		}
	}
}
#endif