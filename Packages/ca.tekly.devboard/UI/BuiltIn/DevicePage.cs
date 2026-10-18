using Tekly.DevBoard.Nodes;
using UnityEngine;

namespace Tekly.DevBoard.BuiltIn
{
	/// <summary>
	/// What the app is running on and as: platform, hardware, graphics and build. Read-only.
	/// </summary>
	internal static class DevicePage
	{
		public const string PATH = "System/Device";

		public static void Register(object owner)
		{
			DevBoard.Page(PATH, Build, owner: owner);
		}

		private static void Build(Segment page)
		{
			Section(page, "Application", section => {
				Row(section, "Version", Application.version);
				Row(section, "Platform", Application.platform.ToString());
				Row(section, "Build", Debug.isDebugBuild ? "Development" : "Release");

#if UNITY_WEBGL && !UNITY_EDITOR
				// The page the build is running on, for telling deployed builds apart
				Row(section, "URL", Application.absoluteURL);
#endif

				section.Property("Network", () => Application.internetReachability.ToString());
			});

			Section(page, "Device", section => {
				Row(section, "Model", SystemInfo.deviceModel);
				Row(section, "OS", SystemInfo.operatingSystem);
				Row(section, "CPU", $"{SystemInfo.processorType} ({SystemInfo.processorCount} threads)");
				Row(section, "Memory", $"{SystemInfo.systemMemorySize:N0} MB");
				Row(section, "Language", Application.systemLanguage.ToString());
			});

			Section(page, "Graphics", section => {
				Row(section, "GPU", SystemInfo.graphicsDeviceName);
				Row(section, "API", SystemInfo.graphicsDeviceType.ToString());
				Row(section, "Memory", $"{SystemInfo.graphicsMemorySize:N0} MB");
				Row(section, "Max Texture", SystemInfo.maxTextureSize.ToString());
			});
		}

		private static void Section(Segment page, string heading, System.Action<ContainerNode> build)
		{
			build(page.Foldout(heading, Styles.SubtleFoldout));
		}

		private static void Row(ContainerNode section, string label, string value)
		{
			section.Property(label, () => value);
		}
	}
}
