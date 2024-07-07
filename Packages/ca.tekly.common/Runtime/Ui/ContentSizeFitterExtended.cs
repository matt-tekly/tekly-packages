using Tekly.WebSockets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FitMode = UnityEngine.UI.ContentSizeFitter.FitMode;

namespace Tekly.Common.Ui
{
	public class ContentSizeFitterExtended : UIBehaviour, ILayoutSelfController, ILayoutGroup
	{
		[SerializeField] private RectTransform m_Target;
		[SerializeField] private Vector2 m_SizeMin = new Vector2(0f, 0f);
		[SerializeField] private Vector2 m_SizeMax = new Vector2(1920f, 1080f);

		[SerializeField] protected FitMode m_HorizontalFit = FitMode.Unconstrained;
		[SerializeField] protected FitMode m_VerticalFit = FitMode.Unconstrained;

		[System.NonSerialized] private RectTransform m_Rect;

#pragma warning disable 649
		private DrivenRectTransformTracker m_Tracker;
#pragma warning restore 649

		/// <summary>
		/// The fit mode to use to determine the width.
		/// </summary>
		public FitMode horizontalFit {
			get => m_HorizontalFit;
			set {
				if (SetPropertyUtility.SetStruct(ref m_HorizontalFit, value)) {
					SetDirty();
				}
			}
		}


		/// <summary>
		/// The fit mode to use to determine the height.
		/// </summary>
		public FitMode verticalFit {
			get => m_VerticalFit;
			set {
				if (SetPropertyUtility.SetStruct(ref m_VerticalFit, value)) {
					SetDirty();
				}
			}
		}

		private RectTransform rectTransform {
			get {
				if (m_Rect == null) {
					m_Rect = GetComponent<RectTransform>();
				}

				return m_Rect;
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			SetDirty();
		}

		protected override void OnDisable()
		{
			m_Tracker.Clear();
			LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
			base.OnDisable();
		}

		protected override void OnRectTransformDimensionsChange()
		{
			SetDirty();
		}

		protected virtual void OnTransformChildrenChanged()
		{
			SetDirty();
		}

		private void HandleSelfFittingAlongAxis(int axis)
		{
			if (m_Target == null) {
				return;
			}

			var fitting = (axis == 0 ? horizontalFit : verticalFit);

			if (fitting == FitMode.Unconstrained) {
				// Keep a reference to the tracked transform, but don't control its properties:
				m_Tracker.Add(this, rectTransform, DrivenTransformProperties.None);
				return;
			}

			m_Tracker.Add(this, rectTransform,
				(axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY));

			var rectAxis = (RectTransform.Axis)axis;
			var clamp = axis == 0 ? new Vector2(m_SizeMin.x, m_SizeMax.x) : new Vector2(m_SizeMin.y, m_SizeMax.y);

			float size;
			
			// Set size to min or preferred size
			if (fitting == FitMode.MinSize) {
				size = Mathf.Clamp(LayoutUtility.GetMinSize(m_Target, axis), clamp.x, clamp.y);
			} else {
				size = Mathf.Clamp(LayoutUtility.GetPreferredSize(m_Target, axis), clamp.x, clamp.y);
			}
			
			rectTransform.SetSizeWithCurrentAnchors(rectAxis, size);
		}

		/// <summary>
		/// Calculate and apply the horizontal component of the size to the RectTransform
		/// </summary>
		public void SetLayoutHorizontal()
		{
			m_Tracker.Clear();
			HandleSelfFittingAlongAxis(0);
		}

		/// <summary>
		/// Calculate and apply the vertical component of the size to the RectTransform
		/// </summary>
		public void SetLayoutVertical()
		{
			HandleSelfFittingAlongAxis(1);
		}

		private void SetDirty()
		{
			if (!IsActive()) {
				return;
			}

			LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			SetDirty();
		}
#endif
	}
}