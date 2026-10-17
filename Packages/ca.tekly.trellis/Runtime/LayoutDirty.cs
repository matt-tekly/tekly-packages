using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Trellis
{
	/// <summary>
	/// The one place Trellis asks Unity for a layout rebuild. Unity refuses (and logs an error) while it is
	/// already rebuilding, so requests made then are held and sent just before the next layout pass, each
	/// rect once. That works in play mode and in the editor alike, with no coroutine involved.
	/// </summary>
	internal static class LayoutDirty
	{
		private static readonly HashSet<RectTransform> s_deferred = new HashSet<RectTransform>();
		private static bool s_hooked;

		/// <summary>
		/// Request a rebuild of the layout tree holding this rect. Safe to call during a rebuild.
		/// </summary>
		public static void Mark(RectTransform rect)
		{
			if (rect == null) {
				return;
			}

			if (!CanvasUpdateRegistry.IsRebuildingLayout()) {
				LayoutRebuilder.MarkLayoutForRebuild(rect);
				return;
			}

			EnsureHooked();
			s_deferred.Add(rect);
		}

		private static void EnsureHooked()
		{
			if (s_hooked) {
				return;
			}

			s_hooked = true;
			Canvas.preWillRenderCanvases += Flush;
		}

		// Runs before CanvasUpdateRegistry does its layout pass, so held requests land in that pass
		private static void Flush()
		{
			if (s_deferred.Count == 0) {
				return;
			}

			foreach (var rect in s_deferred) {
				if (rect != null) {
					LayoutRebuilder.MarkLayoutForRebuild(rect);
				}
			}

			s_deferred.Clear();
		}
	}
}
