using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Tekly.Common.Utils;
using Tekly.Common.LifeCycles;
using Tekly.Logging.Configurations;
using Tekly.Logging.LogDestinations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Logging
{
    public partial class TkLogger
    {
        public static bool EnableStackTrace = true;

        public static IEnumerable<ILogDestination> Destinations => s_logDestinations.Values;
        
        public static readonly TkLoggerStats Stats = new TkLoggerStats();
        public static event Action<TkLogMessage> MessageLogged;

        public static readonly ConcurrentDictionary<string, string> CommonFields = new ConcurrentDictionary<string, string>();

        private static readonly Dictionary<string, ILogDestination> s_logDestinations = new Dictionary<string, ILogDestination>();
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

        public static void Initialize()
        {
            var profile = Resources.Load<LoggingProfileConfig>("Logging/default_profile");
            
            if (profile != null) {
                Initialize(profile);
            } else {
                ApplyDefaultProfile();
            }
        }

        public static void Initialize(LoggingProfileConfig config)
        {
            foreach (var destination in Destinations) {
                destination.Dispose();
            }
            
            s_logDestinations.Clear();
            s_settingsTree.Initialize(config.DefaultLevel, config.DefaultDestination, config.Configs);

            foreach (var logger in s_loggers) {
                logger.Value.LoggerSettings = s_settingsTree.GetSettings(logger.Key.FullName);
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeCommon()
        {
            Application.logMessageReceivedThreaded += HandleUnityLog;
            LifeCycle.Instance.Update += Update;

            UpdateCommonProperties();
            UnityRuntimeEditorUtils.OnExitPlayMode(OnEditorStopPlaying);
        }

        private static void Reset()
        {
            foreach (var destination in Destinations) {
                destination.Dispose();
            }
            
            s_logDestinations.Clear();
            CommonFields.Clear();
            Stats.Clear();
            
            s_settingsTree.Clear();
            
            Application.logMessageReceivedThreaded -= HandleUnityLog;
            LifeCycle.Instance.Update -= Update;
            
            MessageLogged = null;
        }

        public static void HardReset()
        {
            Reset();
            s_loggers.Clear();
        }

        public static ILogDestination GetDestination(LogDestinationConfig config)
        {
            if (!s_logDestinations.TryGetValue(config.name, out var destination)) {
                destination = config.CreateInstance();
                s_logDestinations[config.name] = destination;
            }

            return destination;
        }
        
        private static TkLogger Create(Type type)
        {
            var settings = s_settingsTree.GetSettings(type.FullName);
            return new TkLogger(type, settings);
        }
        
        public static void UpdateCommonProperties()
        {
            SetCommonField("_frame", s_frame);
            SetCommonField("_realTime", s_realtimeSinceStartup);
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
            var logMessage = new TkLogMessage(level, loggerName, loggerName, message, stacktrace);

            foreach (var logDestination in Destinations) {
                logDestination.LogMessage(logMessage, LogSource.Unity);
            }
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

        private static void LogToDestination(ILogDestination destination, TkLogMessage message, LogSource logSource = LogSource.TkLogger)
        {
            Stats.Track(message);
            destination.LogMessage(message, logSource);
            MessageLogged?.Invoke(message);
        }

        private static void LogToDestination(ILogDestination destination, TkLogMessage message, Object context, LogSource logSource = LogSource.TkLogger)
        {
            Stats.Track(message);
            destination.LogMessage(message, context, logSource);
            MessageLogged?.Invoke(message);
        }

        private static void ApplyDefaultProfile()
        {
            foreach (var destination in Destinations) {
                destination.Dispose();
            }
            
            s_logDestinations.Clear();

            var unityLogDestination = new UnityLogDestination("unity", new [] { LogPrefixes.Frame, LogPrefixes.Logger});
            s_logDestinations.Add("unity", unityLogDestination);
            
            s_settingsTree.Initialize(TkLogLevel.Debug, unityLogDestination, Array.Empty<LoggerConfig>());

            foreach (var logger in s_loggers) {
                logger.Value.LoggerSettings = s_settingsTree.GetSettings(logger.Key.FullName);
            }
        }
        
        private static void OnEditorStopPlaying()
        {
            Reset();
            ApplyDefaultProfile();
        }
        
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }
            
            ApplyDefaultProfile();
        }
#endif
    }
}