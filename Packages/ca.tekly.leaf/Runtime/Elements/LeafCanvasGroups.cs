using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// CanvasGroup checks for Leaf components that aren't Selectables, which Unity doesn't track groups for.
	/// </summary>
	public static class LeafCanvasGroups
	{
		private static readonly List<CanvasGroup> s_canvasGroupCache = new();

		/// <summary>
		/// False when an enabled CanvasGroup on or above transform isn't interactable, stopping at groups that
		/// ignore their parents. Matches Selectable's check.
		/// </summary>
		public static bool AllowInteraction(Transform transform)
		{
			var t = transform;
			while (t != null) {
				t.GetComponents(s_canvasGroupCache);
				foreach (var canvasGroup in s_canvasGroupCache) {
					if (canvasGroup.enabled && !canvasGroup.interactable) {
						s_canvasGroupCache.Clear();
						return false;
					}

					if (canvasGroup.ignoreParentGroups) {
						s_canvasGroupCache.Clear();
						return true;
					}
				}

				t = t.parent;
			}

			s_canvasGroupCache.Clear();
			return true;
		}
	}
}
