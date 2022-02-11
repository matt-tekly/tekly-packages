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

	public delegate void ApplicationInfoDelegate(List<InfoItem> items);

	[Serializable]
	public class InfoSummary
	{
		public List<InfoItem> Info = new List<InfoItem>();

		public InfoSummary()
		{
			AddAppInfo(Info);
			AddDeviceInfo(Info);
			AddDeviceSettings(Info);
			AddTimeInfo(Info);

			ApplicationInfoRequested?.Invoke(Info);
		}

		public static event ApplicationInfoDelegate ApplicationInfoRequested;

		private static void AddAppInfo(List<InfoItem> items)
		{
			items.Add(new InfoItem("AppInfo", "App Version", Application.version));
			items.Add(new InfoItem("AppInfo", "Build Guid", Application.buildGUID));
			items.Add(new InfoItem("AppInfo", "Company Name", Application.companyName));
			items.Add(new InfoItem("AppInfo", "Identifier", Application.identifier));
			items.Add(new InfoItem("AppInfo", "Platform", Application.platform.ToString()));
			items.Add(new InfoItem("AppInfo", "Persistent Data Path", Application.persistentDataPath));
			items.Add(new InfoItem("AppInfo", "Product Name", Application.productName));
			items.Add(new InfoItem("AppInfo", "System Language", Application.systemLanguage.ToString()));
			items.Add(new InfoItem("AppInfo", "Unity Version", Application.unityVersion));
		}

		private static void AddDeviceInfo(List<InfoItem> items)
		{
			items.Add(new InfoItem("Device", "Id", SystemInfo.deviceUniqueIdentifier));
			items.Add(new InfoItem("Device", "Model", SystemInfo.deviceModel));
			items.Add(new InfoItem("Device", "MemoryMb", SystemInfo.systemMemorySize));
			items.Add(new InfoItem("Device", "Name", SystemInfo.deviceName));
			items.Add(new InfoItem("Device", "OS", SystemInfo.operatingSystem));
		}

		private static void AddDeviceSettings(List<InfoItem> items)
		{
			items.Add(new InfoItem("Device Settings", "Run In Background", Application.runInBackground));
			items.Add(new InfoItem("Device Settings", "Target Frame Rate", Application.targetFrameRate));
		}

		private static void AddTimeInfo(List<InfoItem> items)
		{
			items.Add(new InfoItem("Time", "Current Frame", Time.frameCount));
			items.Add(new InfoItem("Time", "Game Time", Time.time));
			items.Add(new InfoItem("Time", "Real Time Since Start Up", Time.realtimeSinceStartup));
			items.Add(new InfoItem("Time", "Rendered Frames", Time.renderedFrameCount));
			items.Add(new InfoItem("Time", "Time Scale", Time.timeScale));
			items.Add(new InfoItem("Time", "Maximum Delta Time", Time.maximumDeltaTime));
		}
	}
}