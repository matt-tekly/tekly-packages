using System.Collections.Generic;
using Tekly.DevBoard.Nodes;
using UnityEngine;

namespace Tekly.DevBoard.BuiltIn
{
	/// <summary>
	/// Screen mode and resolution, and the quality level.
	/// </summary>
	internal static class DisplayPage
	{
		public const string PATH = "System/Display";

		public static void Register(object owner)
		{
			DevBoard.Page(PATH, Build, owner: owner);
		}

		private static void Build(Segment page)
		{
			page.Heading("Screen");
			var form = page.Form();
			form.Property("Window", () => $"{Screen.width} x {Screen.height}").WithMonospaced();
			form.Property("DPI", () => Screen.dpi, 1f, "{0:F0}").WithMonospaced();
			form.Property("Refresh", () => (float) Screen.currentResolution.refreshRateRatio.value, 0.5f, "{0:F0} Hz").WithMonospaced();
			form.Dropdown("Mode", () => Screen.fullScreenMode, mode => Screen.fullScreenMode = mode);

			var resolutions = Resolutions();

			if (resolutions.Count > 1) {
				form.Dropdown("Resolution", resolutions, size => $"{size.x} x {size.y}", CurrentResolution, SetResolution);
			}

			page.Heading("Quality");
			form = page.Form();
			var levels = QualitySettings.names;

			if (levels.Length > 0) {
				form.Dropdown("Level", levels, () => levels[QualitySettings.GetQualityLevel()],
					name => QualitySettings.SetQualityLevel(System.Array.IndexOf(levels, name), true));
			}

			form.Slider("LOD Bias", 0.1f, 4f, () => QualitySettings.lodBias, value => QualitySettings.lodBias = value).WithFormat("0.00");
			form.Dropdown("Anti-Aliasing", AntiAliasingLevels, level => level == 0 ? "Off" : $"{level}x",
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
