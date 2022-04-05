using System.IO;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace Tekly.Common.Terminal.Commands
{
    public class BasicCommands : ICommandSource
    {
        [Command("quit")]
        [Help("Quits the game")]
        public void Quit(int code = 0)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(code);
#endif
        }

        [Command("app.log")]
        [Help("Logs a message")]
        public void Log(string message, LogType logType = LogType.Log)
        {
            Debug.unityLogger.Log(logType, message);
        }
        
#if UNITY_EDITOR || UNITY_STANDALONE
        [Command("app.logfile")]
        [Help("Get the log file")]
        public string LogFile()
        {
            return Application.consoleLogPath;
        }
#endif
        
        [Command("app.fps")]
        [Help("Get or set the target FPS")]
        public string Fps(int? targetFps)
        {
            if (targetFps.HasValue) {
                Application.targetFrameRate = targetFps.Value;    
            }
            
            return Application.targetFrameRate.ToString();
        }
        
        [Command("app.timescale")]
        [Help("Sets or gets the time scale")]
        public string TimeScale(float? timeScale)
        {
            if (timeScale.HasValue) {
                Time.timeScale = timeScale.Value;
            }
            
            return Time.timeScale.ToString("0.000");
        }
        
        [Command("app.device.info")]
        [Help("Get device information")]
        public string DeviceInfo()
        {
            var displayList = new DisplayList();
            
            displayList.Add("Name", SystemInfo.deviceName);
            displayList.Add("Model", SystemInfo.deviceModel);
            displayList.Add("Device Id", SystemInfo.deviceUniqueIdentifier);
            displayList.Add("Memory", $"{SystemInfo.systemMemorySize}MB");
            displayList.Add("Screen", $"{Screen.width} x {Screen.height}");
            displayList.Add("OS", SystemInfo.operatingSystem);

            return displayList.ToString();
        }
        
        [Command("app.info")]
        [Help("Get application info")]
        public string ApplicationInfo()
        {
            var displayList = new DisplayList();
            
            displayList.Add("App Version", Application.version);
            displayList.Add("Build Guid", Application.buildGUID);
            displayList.Add("Company Name", Application.companyName);
            displayList.Add("Identifier", Application.identifier);
            displayList.Add("Platform", Application.platform.ToString());
            displayList.Add("Persistent Data Path", Application.persistentDataPath);
            displayList.Add("Product Name", Application.productName);
            displayList.Add("System Language", Application.systemLanguage.ToString());
            displayList.Add("Unity Version", Application.unityVersion);

            return displayList.ToString();
        }

        [Command("app.screen.size")]
        [Help("Get screen size")]
        public string ScreenSize()
        {
            return $"{Screen.width} x {Screen.height}";
        }
        
        [Command("app.screen.fullscreen")]
        [Help("Get or full screen")]
        public string FullScreen(bool? fullScreen)
        {
            if (fullScreen.HasValue) {
                Screen.fullScreen = fullScreen.Value;
            }
            
            return Screen.fullScreen.ToString();
        }

        [Command("app.crash")]
        [Help("Force the app to crash")]
        public void Crash(ForcedCrashCategory forcedCrashCategory)
        {
            UnityEngine.Diagnostics.Utils.ForceCrash(forcedCrashCategory);
        }
        
        [Command("app.open")]
        [Help("Open a thing")]
        public void Open(string message)
        {
            Application.OpenURL(message);
        }
    }
}