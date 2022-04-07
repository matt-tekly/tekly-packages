using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public static TkLogLevel GlobalMinLogLevel { get; private set; }

        public static bool EnableUnityLogger = TkLoggerConstants.UNITY_LOG_ENABLED_DEFAULT;

        public static readonly List<ITkLogDestination> Destinations = new List<ITkLogDestination>();

        public static readonly ConcurrentDictionary<string, string> CommonFields = new ConcurrentDictionary<string, string>();

        private static UnityLogDestination s_unityLogDestination;

        private static readonly ConcurrentDictionary<Type, TkLogger> s_loggers = new ConcurrentDictionary<Type, TkLogger>();

        private static readonly ThreadLocal<StringBuilder> s_stringBuilders = new ThreadLocal<StringBuilder>(() => new StringBuilder(512));

        private static readonly TkLogLevelsTree s_levelsTree = new TkLogLevelsTree();

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
        
        private static TkLogger Create(Type type)
        {
            var level = s_levelsTree.GetLevel(type.FullName);
            return new TkLogger(type, level);
        }

        public static void SetGlobalMinLogLevel(TkLogLevel level)
        {
            GlobalMinLogLevel = level;
            foreach (var logger in s_loggers.Values) {
                var newLevel = s_levelsTree.GetLevel(logger.GetType().FullName);
                logger.OverrideMinLogLevel(newLevel);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Initialize()
        {
            GlobalMinLogLevel = TkLogLevel.Info;

            LoadResourcesConfig();
            LoadLocalFileConfig();

            s_unityLogDestination = new UnityLogDestination();
            Application.logMessageReceivedThreaded += HandleUnityLog;
            LifeCycle.Instance.Update += Update;

            UpdateCommonProperties();
            UnityRuntimeEditorUtils.OnExitPlayMode(Reset);
        }

        public static void Reset()
        {
            foreach (var destination in Destinations) {
                destination.Dispose();
            }
            
            Destinations.Clear();
            CommonFields.Clear();
            
            s_loggers.Clear();
            s_levelsTree.Clear();
        }

        private static void LoadResourcesConfig()
        {
            var configAsset = Resources.Load<TextAsset>("logger_config");

            if (configAsset != null) {
                var loggerConfig = LoggerConfig.DeserializeXml(configAsset.text);
                ApplyConfig(loggerConfig);
            } else {
                Debug.LogError("Failed to find a 'logger_config.xml' in Resources directory. Please create one");
            }
        }

        private static void LoadLocalFileConfig()
        {
            if (LocalFile.Exists("Settings/logger_config.xml")) {
                var xml = LocalFile.ReadAllText("Settings/logger_config.xml");
                var loggerConfig = LoggerConfig.DeserializeXml(xml);
                ApplyConfig(loggerConfig);
            }
        }

        public static void ApplyConfig(LoggerConfig loggerConfig)
        {
            s_levelsTree.Initialize(loggerConfig.DefaultProfile.Levels);
        }

        public static void UpdateCommonProperties()
        {
            SetCommonField("_frame", s_frame);
            SetCommonField("_realTime", s_realtimeSinceStartup);
        }

        private static void Update()
        {
            if (EnableUnityLogger) {
                s_unityLogDestination.Update();
            }

            s_frame = Time.frameCount;
            s_realtimeSinceStartup = Time.realtimeSinceStartup;

            foreach (var destination in Destinations) {
                destination.Update();
            }
        }

        private static void HandleUnityLog(string message, string stacktrace, LogType type)
        {
            if (message[message.Length - 1] == TkLoggerConstants.UNITY_LOG_MARKER) {
                return;
            }

            var level = UnityLogDestination.TypeToLevel(type);
            var loggerName = TkLoggerConstants.UNITY_LOG_NAME;
            stacktrace = stacktrace.Replace("\\", "/");
            LogToDestinations(new TkLogMessage(level, loggerName, loggerName, message, stacktrace), false);
        }

        public static void SetCommonField(string id, object value)
        {
            CommonFields[id] = value.ToString();
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

            sb.Replace("\\", "/");

            return sb.ToString();
        }

        private static void LogToDestinations(TkLogMessage message, bool logToUnity = true)
        {
            if (EnableUnityLogger && logToUnity) {
                s_unityLogDestination.LogMessage(message);
            }

            foreach (var destination in Destinations) {
                destination.LogMessage(message);
            }
        }

        private static void LogToDestinations(TkLogMessage message, Object context, bool logToUnity = true)
        {
            if (EnableUnityLogger && logToUnity) {
                s_unityLogDestination.LogMessage(message, context);
            }

            foreach (var destination in Destinations) {
                destination.LogMessage(message, context);
            }
        }
    }
}