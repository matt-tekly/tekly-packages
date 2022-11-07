using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Common.Ui
{
	[Serializable]
	public struct SafeAreaOffsetSettings
	{
		public Vector2 OffsetMin;
		public Vector2 OffsetMax;
	}
	
	[ExecuteAlways]
	public class SafeAreaSizer : UIBehaviour, ILayoutSelfController
	{
		[SerializeField] private bool m_ignoreBottom;
		[SerializeField] private bool m_ignoreTop;
		
		[SerializeField] private SafeAreaOffsetSettings m_active;
		[SerializeField] private SafeAreaOffsetSettings m_inactive;

		private Rect m_lastSafeArea;

		private const DrivenTransformProperties DRIVEN_PROPERTIES = DrivenTransformProperties.Anchors;

		private DrivenRectTransformTracker m_tracker;
		private RectTransform m_rectTransform;

		private RectTransform RectTransform {
			get {
				if (m_rectTransform == null) {
					m_rectTransform = GetComponent<RectTransform>();
				}

				return m_rectTransform;
			}
		}
		
		public void SetLayoutHorizontal()
		{
			DoLayout();
		}

		public void SetLayoutVertical()
		{
			DoLayout();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			m_tracker.Clear();

			m_tracker.Add(this, RectTransform, DRIVEN_PROPERTIES);

			DoLayout();

			LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
		}

		protected override void OnDisable()
		{
			m_tracker.Clear();
		}
		
		private void Update()
		{
			var safeArea = UnityEngine.Device.Screen.safeArea;
			
			if (m_lastSafeArea != safeArea) {
				ApplySafeArea(safeArea);
			}
		}
		
		private void DoLayout()
		{
			ApplySafeArea(UnityEngine.Device.Screen.safeArea);
		}

		private void ApplySafeArea(Rect area)
		{
			var anchorMin = area.position;
			var anchorMax = area.position + area.size;

			anchorMin.x /= UnityEngine.Device.Screen.width;
			anchorMin.y /= UnityEngine.Device.Screen.height;
			anchorMax.x /= UnityEngine.Device.Screen.width;
			anchorMax.y /= UnityEngine.Device.Screen.height;

			if (m_ignoreBottom) {
				anchorMin.x = 0;
				anchorMin.y = 0;
			}

			if (m_ignoreTop) {
				anchorMax.x = 1;
				anchorMax.y = 1;
			}

			var rt = RectTransform;
			rt.anchorMin = anchorMin;
			rt.anchorMax = anchorMax;

			m_lastSafeArea = area;

			var isTopOffset = !m_ignoreTop && !Mathf.Approximately(area.y + area.height, UnityEngine.Device.Screen.height);
			var isBottomOffset = !m_ignoreBottom && Mathf.Approximately(area.y, 0);

			rt.offsetMax = isTopOffset ? m_active.OffsetMax : m_inactive.OffsetMax;
			rt.offsetMin = isBottomOffset ? m_active.OffsetMin : m_inactive.OffsetMin;
		}
		
#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
		}
#endif
		
	}
}