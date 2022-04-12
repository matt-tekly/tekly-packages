//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Webster.Routes
{
    [Serializable]
    public class InfoItem
    {
        public string Category;
        public string Name;
        public string Value;

        public InfoItem(string category, string name, string value)
        {
            Category = category;
            Name = name;
            Value = value;
        }

        public InfoItem(string category, string name, float value)
        {
            Category = category;
            Name = name;
            Value = value.ToString("0.00");
        }

        public InfoItem(string category, string name, int value)
        {
            Category = category;
            Name = name;
            Value = value.ToString();
        }

        public InfoItem(string category, string name, bool value)
        {
            Category = category;
            Name = name;
            Value = value.ToString();
        }
    }

    public delegate void ApplicationInfoDelegate(InfoSummary summary);

    [Serializable]
    public class InfoSummary
    {
        public List<InfoItem> Info = new List<InfoItem>();

        private InfoSummary() { }

        public static event ApplicationInfoDelegate ApplicationInfoRequested;
        
        public static InfoSummary Get()
        {
            var summary = new InfoSummary();

            AddAppInfo(summary);
            AddDeviceInfo(summary);
            AddDeviceSettings(summary);
            AddTimeInfo(summary);

            ApplicationInfoRequested?.Invoke(summary);

            return summary;
        }
        
        public void Add(string category, string name, string value)
        {
            Info.Add(new InfoItem(category, name, value));
        }

        public void Add(string category, string name, float value)
        {
            Info.Add(new InfoItem(category, name, value));
        }

        public void Add(string category, string name, int value)
        {
            Info.Add(new InfoItem(category, name, value));
        }

        public void Add(string category, string name, bool value)
        {
            Info.Add(new InfoItem(category, name, value));
        }

        private static void AddAppInfo(InfoSummary summary)
        {
            summary.Add("AppInfo", "App Version", Application.version);
            summary.Add("AppInfo", "Build Guid", Application.buildGUID);
            summary.Add("AppInfo", "Company Name", Application.companyName);
            summary.Add("AppInfo", "Identifier", Application.identifier);
            summary.Add("AppInfo", "Platform", Application.platform.ToString());
            summary.Add("AppInfo", "Persistent Data Path", Application.persistentDataPath);
            summary.Add("AppInfo", "Product Name", Application.productName);
            summary.Add("AppInfo", "System Language", Application.systemLanguage.ToString());
            summary.Add("AppInfo", "Unity Version", Application.unityVersion);
        }

        private static void AddDeviceInfo(InfoSummary summary)
        {
            summary.Add("Device", "Id", SystemInfo.deviceUniqueIdentifier);
            summary.Add("Device", "Model", SystemInfo.deviceModel);
            summary.Add("Device", "MemoryMb", SystemInfo.systemMemorySize);
            summary.Add("Device", "Name", SystemInfo.deviceName);
            summary.Add("Device", "OS", SystemInfo.operatingSystem);
        }

        private static void AddDeviceSettings(InfoSummary summary)
        {
            summary.Add("Device Settings", "Run In Background", Application.runInBackground);
            summary.Add("Device Settings", "Target Frame Rate", Application.targetFrameRate);
        }

        private static void AddTimeInfo(InfoSummary summary)
        {
            summary.Add("Time", "Current Frame", Time.frameCount);
            summary.Add("Time", "Game Time", Time.time);
            summary.Add("Time", "Real Time Since Start Up", Time.realtimeSinceStartup);
            summary.Add("Time", "Rendered Frames", Time.renderedFrameCount);
            summary.Add("Time", "Time Scale", Time.timeScale);
            summary.Add("Time", "Maximum Delta Time", Time.maximumDeltaTime);
        }
        
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            ApplicationInfoRequested = null;
        }
#endif
    }
}