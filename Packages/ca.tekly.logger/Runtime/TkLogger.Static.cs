using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Tekly.Common.LocalFiles;
using Tekly.Common.Utils;
using Tekly.Common.LifeCycles;
using Tekly.Logging.LogDestinations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Logging
{
    public partial class TkLogger
    {
        public static bool EnableStackTrace = true;
        
        public static readonly List<ILogDestination> Destinations = new List<ILogDestination>();
        public static readonly List<LoggerGroup> Groups = new List<LoggerGroup>();
        public static LoggerGroup DefaultGroup;

        public static readonly ConcurrentDictionary<string, string> CommonFields = new ConcurrentDictionary<string, string>();
        
        private static readonly ConcurrentDictionary<Type, TkLogger> s_loggers = new ConcurrentDictionary<Type, TkLogger>();

        private static readonly ThreadLocal<StringBuilder> s_stringBuilders = new ThreadLocal<StringBuilder>(() => new StringBuilder(512));

        private static readonly LogSettingsTree s_settingsTree = new LogSettingsTree();

        private static int s_frame;
        private static float s_realtimeSinceStartup;

        public static TkLogger Get<T>()
        {
            return Get(typeof(T));
        }

        public static TkLogger Get(Type type)
        {
            return s_loggers.GetOrAdd(type, Create);
        }
        
        public static void Initialize(string profile = null)
        {
            LoadConfigFile(profile);
            InitializeCommon();
        }
        
        public static void Initialize(LoggerConfigData config, string profile)
        {
            ApplyConfig(config, profile);
            InitializeCommon();
        }
        
        public static void Initialize(LoggerProfileData profile)
        {
            ApplyProfile(profile);
            InitializeCommon();
        }

        private static void InitializeCommon()
        {
            Application.logMessageReceivedThreaded += HandleUnityLog;
            LifeCycle.Instance.Update += Update;

            UpdateCommonProperties();
            UnityRuntimeEditorUtils.OnExitPlayMode(OnEditorStopPlaying);
        }

        private static void LoadConfigFile(string profile)
        {
            if (LocalFile.Exists("user/logger_config.xml")) {
                var xml = LocalFile.ReadAllText("user/logger_config.xml");
                var loggerConfig = LoggerConfigData.DeserializeXml(xml);
                ApplyConfig(loggerConfig, profile);
            } else {
                var configAsset = Resources.Load<TextAsset>("logger_config");

                if (configAsset != null) {
                    var loggerConfig = LoggerConfigData.DeserializeXml(configAsset.text);
                    ApplyConfig(loggerConfig, profile);
                } else {
                    UnityEngine.Debug.LogError("Failed to find a 'logger_config.xml' in Resources directory. Please create one");
                    ApplyProfile(CreateDefaultProfile());
                }
            }
        }

        public static void Reset()
        {
            foreach (var destination in Destinations) {
                destination.Dispose();
            }
            
            Destinations.Clear();
            CommonFields.Clear();
            Groups.Clear();

            s_loggers.Clear();
            s_settingsTree.Clear();
            
            Application.logMessageReceivedThreaded -= HandleUnityLog;
            LifeCycle.Instance.Update -= Update;
            
        }
        
        private static TkLogger Create(Type type)
        {
            var settings = s_settingsTree.GetSettings(type.FullName);
            return new TkLogger(type, settings);
        }
        
        private static void ApplyConfig(LoggerConfigData loggerConfigData, string profile)
        {
            var targetProfileData = loggerConfigData.Profiles.FirstOrDefault(x => x.Default);
            
            if (!string.IsNullOrEmpty(profile)) {

                foreach (var profileData in loggerConfigData.Profiles) {
                    if (profileData.Name == profile) {
                        targetProfileData = profileData;
                        break;
                    }
                }

                if (targetProfileData == null) {
                    UnityEngine.Debug.LogError($"Didn't find LoggerProfile [{profile}] and there is no default.");
                }
            }

            if (targetProfileData == null) {
                UnityEngine.Debug.LogError("Using built in default Logger Profile");
                targetProfileData = CreateDefaultProfile();
            }
            
            ApplyProfile(targetProfileData);
        }
        
        private static void ApplyProfile(LoggerProfileData loggerProfileData)
        {
            foreach (var destination in Destinations) {
                destination.Dispose();
            }
            
            Destinations.Clear();
            Groups.Clear();

            foreach (var logDestinationConfig in loggerProfileData.Destinations) {
                Destinations.Add(logDestinationConfig.CreateInstance());
            }

            foreach (var groupData in loggerProfileData.Groups) {
                var group = new LoggerGroup();
                group.Name = groupData.Name;
                
                foreach (var destination in groupData.Destinations) {
                    group.Destinations.Add(GetDestination(destination));    
                }

                if (groupData.Default) {
                    DefaultGroup = group;
                }
                
                Groups.Add(group);
            }
            
            s_settingsTree.Initialize(loggerProfileData.Loggers);

            foreach (var logger in s_loggers) {
                logger.Value.LoggerSettings = s_settingsTree.GetSettings(logger.Key.FullName);
            }
        }

        public static void UpdateCommonProperties()
        {
            SetCommonField("_frame", s_frame);
            SetCommonField("_realTime", s_realtimeSinceStartup);
        }
        
        public static LoggerGroup GetGroup(string name)
        {
            foreach (var group in Groups) {
                if (group.Name == name) {
                    return group;
                }
            }

            throw new Exception($"Failed to find LoggerGroup: [{name}]");
        }

        private static ILogDestination GetDestination(string name)
        {
            foreach (var destination in Destinations) {
                if (destination.Name == name) {
                    return destination;
                }
            }

            throw new Exception($"Failed to find LogDestination: [{name}]");
        }

        private static void Update()
        {
            s_frame = Time.frameCount;
            s_realtimeSinceStartup = Time.realtimeSinceStartup;

            foreach (var destination in Destinations) {
                destination.Update();
            }
        }

        private static void HandleUnityLog(string message, string stacktrace, LogType type)
        {
            if (!string.IsNullOrEmpty(message) && message[message.Length - 1] == LoggerConstants.UNITY_LOG_MARKER) {
                return;
            }

            var level = UnityLogDestination.TypeToLevel(type);
            var loggerName = LoggerConstants.UNITY_LOG_NAME;
            
            LogToDestinations(DefaultGroup, new TkLogMessage(level, loggerName, loggerName, message, stacktrace), LogSource.Unity);
        }

        public static void SetCommonField(string id, object value)
        {
            CommonFields[id] = value.ToString();
        }
        
        public static void SetCommonField(string id, int value)
        {
            CommonFields[id] = value.ToString();
        }
        
        public static void SetCommonField(string id, float value)
        {
            CommonFields[id] = value.ToString(CultureInfo.InvariantCulture);
        }
        
        public static void SetCommonField(string id, double value)
        {
            CommonFields[id] = value.ToString(CultureInfo.InvariantCulture);
        }
        
        public static void SetCommonField(string id, bool value)
        {
            CommonFields[id] = value ? "true" : "false";
        }

        public static void ClearCommonField(string id)
        {
            CommonFields.TryRemove(id, out _);
        }

        private static string GetStackTrace()
        {
            if (!EnableStackTrace) {
                return null;
            }

            var sb = s_stringBuilders.Value;
            sb.Clear();

            StackTraceUtilities.ExtractStackTrace(sb, 4);

            return sb.ToString();
        }

        private static void LogToDestinations(LoggerGroup loggerGroup, TkLogMessage message, LogSource logSource = LogSource.TkLogger)
        {
            foreach (var destination in loggerGroup.Destinations) {
                destination.LogMessage(message, logSource);
            }
        }

        private static void LogToDestinations(LoggerGroup loggerGroup, TkLogMessage message, Object context, LogSource logSource = LogSource.TkLogger)
        {
            foreach (var destination in loggerGroup.Destinations) {
                destination.LogMessage(message, context, logSource);
            }
        }

        private static LoggerProfileData CreateDefaultProfile()
        {
            var profileData = new LoggerProfileData();
            profileData.Destinations.Add(new UnityLogDestinationConfig {
                Name = "unity"
            });
            
            profileData.Groups.Add(new LoggerGroupData {
                Name = "default",
                Default = true,
                Destinations = new List<string> {
                    "unity"
                }
            });

            profileData.Loggers = new LoggersData {
                Default = new LoggerSettingsData {
                    Level = TkLogLevel.Info,
                    Group = "default"
                }
            };

            return profileData;
        }
        
        private static void OnEditorStopPlaying()
        {
            Reset();
            ApplyProfile(CreateDefaultProfile());
        }
        
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }
            
            ApplyProfile(CreateDefaultProfile());
        }
#endif
    }
}