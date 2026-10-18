using System.Collections.Generic;
using Tekly.DevBoard.Nodes;
using UnityEngine;

namespace Tekly.DevBoard.BuiltIn
{
	/// <summary>
	/// Screen mode and resolution, frame rate limits, and the quality level.
	/// </summary>
	internal static class DisplayPage
	{
		public const string PATH = "System/Display";

		private static readonly int[] s_frameRates = { -1, 30, 60, 90, 120, 144 };

		private static readonly int[] s_vSyncCounts = { 0, 1, 2 };

		public static void Register(object owner)
		{
			DevBoard.Page(PATH, Build, owner: owner);
		}

		private static void Build(Segment page)
		{
			page.Heading("Screen");
			page.Property("Window", () => $"{Screen.width} x {Screen.height}").WithMonospaced();
			page.Property("DPI", () => Screen.dpi, 1f, "{0:F0}").WithMonospaced();
			page.Property("Refresh", () => (float) Screen.currentResolution.refreshRateRatio.value, 0.5f, "{0:F0} Hz").WithMonospaced();
			page.Dropdown("Mode", () => Screen.fullScreenMode, mode => Screen.fullScreenMode = mode);

			var resolutions = Resolutions();

			if (resolutions.Count > 1) {
				page.Dropdown("Resolution", resolutions, size => $"{size.x} x {size.y}", CurrentResolution, SetResolution);
			}

			// Next to the refresh rate, which VSync locks the frame rate to
			page.Heading("Frame Rate");
			// A rate that isn't one of the choices, set by the game or typed in Custom, shows as no choice. Custom
			// takes any rate, -1 for the platform default
			page.Dropdown("Target", s_frameRates, rate => rate < 0 ? "Default" : rate.ToString(),
				() => Application.targetFrameRate, rate => Application.targetFrameRate = rate);
			page.IntInput("Custom", () => Application.targetFrameRate, rate => Application.targetFrameRate = rate);
			page.Dropdown("VSync", s_vSyncCounts, count => count == 0 ? "Off" : count == 1 ? "Every Frame" : "Every Second Frame",
				() => Mathf.Clamp(QualitySettings.vSyncCount, 0, 2), count => QualitySettings.vSyncCount = count);

			page.Heading("Quality");
			var levels = QualitySettings.names;

			if (levels.Length > 0) {
				page.Dropdown("Level", levels, () => levels[QualitySettings.GetQualityLevel()],
					name => QualitySettings.SetQualityLevel(System.Array.IndexOf(levels, name), true));
			}

			page.Slider("LOD Bias", 0.1f, 4f, () => QualitySettings.lodBias, value => QualitySettings.lodBias = value).WithFormat("0.00");
			page.Dropdown("Anti-Aliasing", AntiAliasingLevels, level => level == 0 ? "Off" : $"{level}x",
				() => QualitySettings.antiAliasing, level => QualitySettings.antiAliasing = level);
		}

		private static readonly int[] AntiAliasingLevels = { 0, 2, 4, 8 };

		/// <summary>
		/// The distinct sizes the display supports, largest first.
		/// </summary>
		private static IReadOnlyList<Vector2Int> Resolutions()
		{
			var sizes = new List<Vector2Int>();

			foreach (var resolution in Screen.resolutions) {
				var size = new Vector2Int(resolution.width, resolution.height);

				if (!sizes.Contains(size)) {
					sizes.Add(size);
				}
			}

			sizes.Sort((a, b) => b.x != a.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));
			return sizes;
		}

		private static Vector2Int CurrentResolution()
		{
			return new Vector2Int(Screen.width, Screen.height);
		}

		private static void SetResolution(Vector2Int size)
		{
			Screen.SetResolution(size.x, size.y, Screen.fullScreenMode);
		}
	}
}
