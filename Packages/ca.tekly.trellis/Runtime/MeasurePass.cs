using Unity.Profiling;
using UnityEngine;

namespace Tekly.Trellis
{
	/// <summary>
	/// A counter that moves on once per frame, just before Unity processes canvases and layout.
	/// Sizes measured during one pass are reused for the rest of it, so a LayoutItem is measured
	/// about once per rebuild instead of every time a parent, a width group or a Unity layout asks.
	/// </summary>
	internal static class MeasurePass
	{
		public static readonly ProfilerMarker MeasureMarker = new ProfilerMarker("Trellis.Measure");
		public static readonly ProfilerMarker CalculateMarker = new ProfilerMarker("Trellis.Calculate");
		public static readonly ProfilerMarker ArrangeMarker = new ProfilerMarker("Trellis.Arrange");

		private static bool s_hooked;

		/// <summary>
		/// The current pass. Never -1, which caches use for "nothing stored".
		/// </summary>
		public static int Current { get; private set; }

		/// <summary>
		/// Subscribe once. Called from LayoutItem.OnEnable so it always runs on the main thread.
		/// </summary>
		public static void EnsureHooked()
		{
			if (s_hooked) {
				return;
			}

			s_hooked = true;
			Canvas.preWillRenderCanvases += Advance;
		}

		private static void Advance()
		{
			Current = Current == int.MaxValue ? 0 : Current + 1;
		}
	}
}
