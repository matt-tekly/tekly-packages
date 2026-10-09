using System;
using Tekly.Common.Ui;
using Tekly.Leaf.Elements;
using Tekly.Trellis;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.DevBoard.Panels
{
	public enum DockSlot
	{
		TopLeft,
		Top,
		TopRight,
		BottomRight,
		Bottom,
		BottomLeft
	}
	
	/// <summary>
	/// The screen-space canvas panels live on, inside the safe area, with a stacking area for each DockSlot.
	/// </summary>
	internal class DevBoardDock
	{
		public const int SORTING_ORDER = 30000;

		private const float MARGIN = 0;
		private const float SPACING = 8f;

		public Canvas Canvas { get; }

		/// <summary>
		/// The safe area panels are laid out in. Its height limits how tall a panel can get.
		/// </summary>
		public RectTransform Area { get; }

		private readonly RectTransform[] m_slots;
		private readonly FixedPhysicalCanvasScaler m_scaler;

		public DevBoardDock(Transform parent)
		{
			var canvasObject = new GameObject("DevBoard Canvas", typeof(RectTransform));
			canvasObject.transform.SetParent(parent, false);
			var navigationScope = canvasObject.AddComponent<LeafNavigationScope>();
			navigationScope.ContainNavigation = true;

			// Debug UI keeps working while the game holds LeafCore.DisableInput
			canvasObject.AddComponent<LeafIgnoreDisableInput>();

			Canvas = canvasObject.AddComponent<Canvas>();
			Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			Canvas.sortingOrder = SORTING_ORDER;
			Canvas.pixelPerfect = true;
			
			m_scaler = canvasObject.AddComponent<FixedPhysicalCanvasScaler>();
			canvasObject.AddComponent<GraphicRaycaster>();

			var areaObject = new GameObject("Safe Area", typeof(RectTransform));
			Area = (RectTransform) areaObject.transform;
			Area.SetParent(canvasObject.transform, false);
			Area.anchorMin = Vector2.zero;
			Area.anchorMax = Vector2.one;
			Area.offsetMin = Vector2.zero;
			Area.offsetMax = Vector2.zero;
			areaObject.AddComponent<SafeAreaSizer>();

			var slots = (DockSlot[]) Enum.GetValues(typeof(DockSlot));
			m_slots = new RectTransform[slots.Length];

			foreach (var slot in slots) {
				m_slots[(int) slot] = CreateSlot(slot);
			}
		}

		public void ApplySettings(DevBoardSettings settings)
		{
			m_scaler.scaleFactor = settings.Scale;
			m_scaler.CompensateGameViewScale = settings.KeepPhysicalSizeInEditor;
		}

		public RectTransform GetSlot(DockSlot slot)
		{
			return m_slots[(int) slot];
		}

		private RectTransform CreateSlot(DockSlot slot)
		{
			var slotObject = new GameObject($"Dock {slot}", typeof(RectTransform));
			var rect = (RectTransform) slotObject.transform;
			rect.SetParent(Area, false);

			var x = HorizontalFactor(slot);
			var y = IsTop(slot) ? 1f : 0f;

			// Anchored and pivoted on its corner or edge, so it grows inwards as panels are added
			rect.anchorMin = new Vector2(x, y);
			rect.anchorMax = new Vector2(x, y);
			rect.pivot = new Vector2(x, y);
			rect.anchoredPosition = new Vector2(MARGIN * (1f - 2f * x), MARGIN * (1f - 2f * y));

			// Centered slots don't step in from the side
			if (Mathf.Approximately(x, 0.5f)) {
				rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
			}

			var layout = slotObject.AddComponent<FlowLayout>();
			layout.Axis = Common.Utils.LayoutAxis.Vertical;
			layout.Spacing = SPACING;
			layout.FitWidth = FitMode.Preferred;
			layout.FitHeight = FitMode.Preferred;
			layout.CrossAlignment = x < 0.5f ? CrossAlignment.Start : Mathf.Approximately(x, 0.5f) ? CrossAlignment.Center : CrossAlignment.End;

			// Bottom slots stack upwards from the bottom edge
			layout.Reverse = !IsTop(slot);

			return rect;
		}

		private static float HorizontalFactor(DockSlot slot)
		{
			switch (slot) {
				case DockSlot.TopLeft:
				case DockSlot.BottomLeft:
					return 0f;
				case DockSlot.Top:
				case DockSlot.Bottom:
					return 0.5f;
				default:
					return 1f;
			}
		}

		private static bool IsTop(DockSlot slot)
		{
			return slot == DockSlot.TopLeft || slot == DockSlot.Top || slot == DockSlot.TopRight;
		}
	}
}
