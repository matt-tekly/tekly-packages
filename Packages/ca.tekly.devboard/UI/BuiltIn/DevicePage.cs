using Tekly.DevBoard.Nodes;
using UnityEngine;

namespace Tekly.DevBoard.BuiltIn
{
	/// <summary>
	/// What the app is running on and as: platform, hardware, graphics and build.
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
			Section(page, "Application", form => {
				Row(form, "Version", Application.version);
				Row(form, "Platform", Application.platform.ToString());
				Row(form, "Build", Debug.isDebugBuild ? "Development" : "Release");

#if UNITY_WEBGL && !UNITY_EDITOR
				// The page the build is running on, for telling deployed builds apart
				Row(form, "URL", Application.absoluteURL);
#endif

				form.Property("Network", () => Application.internetReachability.ToString());
				form.Property("Focused", () => Application.isFocused ? "Yes" : "No");
				form.Toggle("Run In Background", () => Application.runInBackground, value => Application.runInBackground = value);
			});

			Section(page, "Device", form => {
				Row(form, "Model", SystemInfo.deviceModel);
				Row(form, "OS", SystemInfo.operatingSystem);
				Row(form, "CPU", $"{SystemInfo.processorType} ({SystemInfo.processorCount} threads)");
				Row(form, "Memory", $"{SystemInfo.systemMemorySize:N0} MB");
				Row(form, "Language", Application.systemLanguage.ToString());
			});

			Section(page, "Graphics", form => {
				Row(form, "GPU", SystemInfo.graphicsDeviceName);
				Row(form, "API", SystemInfo.graphicsDeviceType.ToString());
				Row(form, "Memory", $"{SystemInfo.graphicsMemorySize:N0} MB");
				Row(form, "Max Texture", SystemInfo.maxTextureSize.ToString());
			});
		}

		private static void Section(Segment page, string heading, System.Action<ContainerNode> build)
		{
			build(page.Foldout(heading, Styles.SubtleFoldout).Form());
		}

		private static void Row(ContainerNode form, string label, string value)
		{
			form.Property(label, () => value);
		}
	}
}
