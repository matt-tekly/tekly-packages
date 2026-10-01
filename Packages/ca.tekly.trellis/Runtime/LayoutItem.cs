using System.Collections;
using System.Collections.Generic;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Tekly.Trellis
{
	/// <summary>
	/// Anything a Trellis layout sizes and places. Replaces Unity's LayoutElement.
	///
	/// A plain LayoutItem is a leaf: its content size comes from the other layout elements on the same
	/// object (Text, Image, ...). Layouts such as FlowLayout inherit from it, so a nested layout is an item
	/// of its parent with no extra component. Unset overrides fall back to the content size.
	///
	/// Rules: min wins over max, preferred is kept between min and max. Margins add to the parent's
	/// spacing and padding; they don't collapse. Max and margins are only understood by Trellis layouts.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	[AddComponentMenu("Layout/Trellis/Layout Item")]
	public class LayoutItem : UIBehaviour, ILayoutElement, ILayoutIgnorer
	{
		private enum SizeKind
		{
			Min,
			Preferred,
			Flexible
		}

		[SerializeField] private bool m_ignoreLayout;

		[SerializeField] private OptionalFloat m_minWidth;
		[SerializeField] private OptionalFloat m_minHeight;
		[SerializeField] private OptionalFloat m_preferredWidth;
		[SerializeField] private OptionalFloat m_preferredHeight;
		[SerializeField] private OptionalFloat m_flexibleWidth;
		[SerializeField] private OptionalFloat m_flexibleHeight;
		[SerializeField] private OptionalFloat m_maxWidth;
		[SerializeField] private OptionalFloat m_maxHeight;

		[SerializeField] private Edges m_margin;

		[Tooltip("Higher priority wins when several layout elements on this object set the same size")]
		[SerializeField] private int m_layoutPriority = 1;

		[Tooltip("Items with the same name under the same WidthGroup share the widest width. Empty = no group")]
		[SerializeField] private string m_widthGroup = "";

		private RectTransform m_rectTransform;

		// Measures reused within a pass. Cleared when this item's settings change and when Unity starts
		// measuring this object in a rebuild, so they're never older than the content they describe.
		private LayoutMeasure m_cachedWidth;
		private LayoutMeasure m_cachedHeight;
		private int m_cachedWidthPass = -1;
		private int m_cachedHeightPass = -1;

		private WidthGroup m_joinedGroup;
		private string m_joinedKey;
		private bool m_measuring;
		private bool m_dirtyPending;

		public bool IgnoreLayout {
			get => m_ignoreLayout;
			set {
				if (SetPropertyUtility.SetStruct(ref m_ignoreLayout, value)) {
					SetDirty();
				}
			}
		}

		public OptionalFloat MinWidth {
			get => m_minWidth;
			set {
				if (SetPropertyUtility.SetStruct(ref m_minWidth, value)) {
					SetDirty();
				}
			}
		}

		public OptionalFloat MinHeight {
			get => m_minHeight;
			set {
				if (SetPropertyUtility.SetStruct(ref m_minHeight, value)) {
					SetDirty();
				}
			}
		}

		public OptionalFloat PreferredWidth {
			get => m_preferredWidth;
			set {
				if (SetPropertyUtility.SetStruct(ref m_preferredWidth, value)) {
					SetDirty();
				}
			}
		}

		public OptionalFloat PreferredHeight {
			get => m_preferredHeight;
			set {
				if (SetPropertyUtility.SetStruct(ref m_preferredHeight, value)) {
					SetDirty();
				}
			}
		}

		public OptionalFloat FlexibleWidth {
			get => m_flexibleWidth;
			set {
				if (SetPropertyUtility.SetStruct(ref m_flexibleWidth, value)) {
					SetDirty();
				}
			}
		}

		public OptionalFloat FlexibleHeight {
			get => m_flexibleHeight;
			set {
				if (SetPropertyUtility.SetStruct(ref m_flexibleHeight, value)) {
					SetDirty();
				}
			}
		}

		public OptionalFloat MaxWidth {
			get => m_maxWidth;
			set {
				if (SetPropertyUtility.SetStruct(ref m_maxWidth, value)) {
					SetDirty();
				}
			}
		}

		public OptionalFloat MaxHeight {
			get => m_maxHeight;
			set {
				if (SetPropertyUtility.SetStruct(ref m_maxHeight, value)) {
					SetDirty();
				}
			}
		}

		public Edges Margin {
			get => m_margin;
			set {
				if (SetPropertyUtility.SetStruct(ref m_margin, value)) {
					SetDirty();
				}
			}
		}

		public int LayoutPriority {
			get => m_layoutPriority;
			set {
				if (SetPropertyUtility.SetStruct(ref m_layoutPriority, value)) {
					SetDirty();
				}
			}
		}

		/// <summary>
		/// Name of the width group this item shares its width with, under the nearest WidthGroup above it.
		/// Empty means no group.
		/// </summary>
		public string WidthGroupName {
			get => m_widthGroup;
			set {
				value = value ?? "";

				if (m_widthGroup != value) {
					m_widthGroup = value;
					RefreshWidthGroup();
					SetDirty();
				}
			}
		}

		/// <summary>
		/// The WidthGroup this item currently shares its width through, if any.
		/// </summary>
		public WidthGroup JoinedWidthGroup => m_joinedGroup;

		protected RectTransform OwnRect {
			get {
				if (m_rectTransform == null) {
					m_rectTransform = (RectTransform) transform;
				}

				return m_rectTransform;
			}
		}

		/// <summary>
		/// Resolved size along an axis (0 = width, 1 = height): content size with this item's overrides,
		/// max and margins applied, and the width shared with its width group. Parents call this while
		/// measuring, after this object's own layout components have run.
		/// </summary>
		public LayoutMeasure Measure(int axis)
		{
			var own = MeasureOwn(axis);

			if (axis != 0 || m_joinedGroup == null) {
				return own;
			}

			m_joinedGroup.Share(m_joinedKey, own, out var min, out var preferred);

			return LayoutMeasure.Create(min, preferred, own.Flexible, own.Max, own.MarginStart, own.MarginEnd);
		}

		/// <summary>
		/// Measure without the width group: what this item would be on its own. Cached for the current pass.
		/// </summary>
		internal LayoutMeasure MeasureOwn(int axis)
		{
			var pass = MeasurePass.Current;

			if (axis == 0) {
				if (m_cachedWidthPass != pass) {
					m_cachedWidth = ComputeOwn(0);
					m_cachedWidthPass = pass;
				}

				return m_cachedWidth;
			}

			if (m_cachedHeightPass != pass) {
				m_cachedHeight = ComputeOwn(1);
				m_cachedHeightPass = pass;
			}

			return m_cachedHeight;
		}

		/// <summary>
		/// Forget cached measures so the next Measure reads the content again. Use after changing something
		/// that affects this item's size without going through its own properties.
		/// </summary>
		public void InvalidateMeasure()
		{
			m_cachedWidthPass = -1;
			m_cachedHeightPass = -1;
		}

		private void InvalidateMeasure(int axis)
		{
			if (axis == 0) {
				m_cachedWidthPass = -1;
			} else {
				m_cachedHeightPass = -1;
			}
		}

		private LayoutMeasure ComputeOwn(int axis)
		{
			using var marker = MeasurePass.MeasureMarker.Auto();

			var content = default(LayoutMeasure);

			// A sibling element that queries LayoutUtility on this object would call back into us
			if (!m_measuring) {
				m_measuring = true;

				try {
					content = MeasureContent(axis);
				} finally {
					m_measuring = false;
				}
			}

			var horizontal = axis == 0;

			var min = Pick(horizontal ? m_minWidth : m_minHeight, content.Min);
			var preferred = Pick(horizontal ? m_preferredWidth : m_preferredHeight, content.Preferred);
			var flexible = Pick(horizontal ? m_flexibleWidth : m_flexibleHeight, content.Flexible);

			return LayoutMeasure.Create(min, preferred, flexible, GetMaxSize(axis),
				m_margin.Start(axis), m_margin.End(axis));
		}

		/// <summary>
		/// Max size along an axis (0 = width, 1 = height), or infinity when unset.
		/// </summary>
		public float GetMaxSize(int axis)
		{
			var max = axis == 0 ? m_maxWidth : m_maxHeight;
			return max.IsSet ? Mathf.Max(0f, max.Value) : float.PositiveInfinity;
		}

		/// <summary>
		/// Request a layout rebuild. Safe to call during a rebuild: it is deferred a frame, once.
		/// </summary>
		public void SetDirty()
		{
			InvalidateMeasure();

			if (!IsActive()) {
				return;
			}

			if (!CanvasUpdateRegistry.IsRebuildingLayout()) {
				LayoutRebuilder.MarkLayoutForRebuild(OwnRect);
				return;
			}

			if (!m_dirtyPending) {
				m_dirtyPending = true;
				StartCoroutine(DelayedSetDirty());
			}
		}

		/// <summary>
		/// Size of what this item holds, before overrides. Leaves ask the other layout elements on this
		/// object; layouts return what they calculated from their children.
		/// </summary>
		protected virtual LayoutMeasure MeasureContent(int axis)
		{
			using (ListPool<Component>.Get(out var components)) {
				GetComponents(typeof(ILayoutElement), components);

				var min = ReadSiblings(components, axis, SizeKind.Min);
				var preferred = ReadSiblings(components, axis, SizeKind.Preferred);
				var flexible = ReadSiblings(components, axis, SizeKind.Flexible);

				return LayoutMeasure.Create(min, Mathf.Max(min, preferred), flexible);
			}
		}

		/// <summary>
		/// Called for this object's measuring passes: 0 = horizontal, 1 = vertical.
		/// </summary>
		protected virtual void OnCalculateLayout(int axis)
		{
		}

		/// <summary>
		/// Join the nearest enabled WidthGroup above this item, or leave the current one.
		/// Called automatically on enable, reparenting and when a WidthGroup turns on or off.
		/// </summary>
		public void RefreshWidthGroup()
		{
			var group = isActiveAndEnabled && !string.IsNullOrEmpty(m_widthGroup) ? FindWidthGroup() : null;
			var key = group != null ? m_widthGroup : null;

			if (group == m_joinedGroup && key == m_joinedKey) {
				return;
			}

			if (m_joinedGroup != null) {
				m_joinedGroup.Remove(this, m_joinedKey);
			}

			m_joinedGroup = group;
			m_joinedKey = key;

			if (m_joinedGroup != null) {
				m_joinedGroup.Add(this, m_joinedKey);
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			MeasurePass.EnsureHooked();
			RefreshWidthGroup();
			SetDirty();
		}

		protected override void OnDisable()
		{
			RefreshWidthGroup();

			// Coroutines stop when disabled, and IsActive is already false here
			m_dirtyPending = false;
			LayoutRebuilder.MarkLayoutForRebuild(OwnRect);
			base.OnDisable();
		}

		protected override void OnTransformParentChanged()
		{
			base.OnTransformParentChanged();
			RefreshWidthGroup();
			SetDirty();
		}

		protected override void OnBeforeTransformParentChanged()
		{
			base.OnBeforeTransformParentChanged();

			// Rebuild the layout we're leaving
			SetDirty();
		}

		protected override void OnDidApplyAnimationProperties()
		{
			base.OnDidApplyAnimationProperties();
			SetDirty();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			m_widthGroup = m_widthGroup ?? "";
			RefreshWidthGroup();
			SetDirty();
		}
#endif

		/// <summary>
		/// Nearest enabled WidthGroup on an ancestor. A group on this object's own GameObject is for its
		/// descendants, not for this item.
		/// </summary>
		private WidthGroup FindWidthGroup()
		{
			for (var current = transform.parent; current != null; current = current.parent) {
				var group = current.GetComponent<WidthGroup>();

				if (group != null && group.isActiveAndEnabled) {
					return group;
				}
			}

			return null;
		}

		// Same selection rule as LayoutUtility: highest priority wins, ties take the largest value,
		// negative means "not set". Skips this component so leaves don't measure themselves.
		private float ReadSiblings(List<Component> components, int axis, SizeKind kind)
		{
			var best = 0f;
			var bestPriority = int.MinValue;

			foreach (var component in components) {
				if (component == this) {
					continue;
				}

				if (component is Behaviour behaviour && !behaviour.isActiveAndEnabled) {
					continue;
				}

				var element = (ILayoutElement) component;
				var value = Read(element, axis, kind);

				if (value < 0f) {
					continue;
				}

				var priority = element.layoutPriority;

				if (priority > bestPriority) {
					best = value;
					bestPriority = priority;
				} else if (priority == bestPriority && value > best) {
					best = value;
				}
			}

			return best;
		}

		private static float Read(ILayoutElement element, int axis, SizeKind kind)
		{
			switch (kind) {
				case SizeKind.Min:
					return axis == 0 ? element.minWidth : element.minHeight;
				case SizeKind.Preferred:
					return axis == 0 ? element.preferredWidth : element.preferredHeight;
				default:
					return axis == 0 ? element.flexibleWidth : element.flexibleHeight;
			}
		}

		private static float Pick(OptionalFloat value, float fallback)
		{
			return value.IsSet ? value.Value : fallback;
		}

		private IEnumerator DelayedSetDirty()
		{
			yield return null;

			m_dirtyPending = false;
			LayoutRebuilder.MarkLayoutForRebuild(OwnRect);
		}

		// ILayoutElement / ILayoutIgnorer, implemented explicitly so the lowercase names stay off the
		// public API. Unity's layout groups and ContentSizeFitter see the resolved sizes (no max or margins).
		bool ILayoutIgnorer.ignoreLayout => m_ignoreLayout;
		float ILayoutElement.minWidth => Measure(0).Min;
		float ILayoutElement.preferredWidth => Measure(0).Preferred;
		float ILayoutElement.flexibleWidth => Measure(0).Flexible;
		float ILayoutElement.minHeight => Measure(1).Min;
		float ILayoutElement.preferredHeight => Measure(1).Preferred;
		float ILayoutElement.flexibleHeight => Measure(1).Flexible;
		int ILayoutElement.layoutPriority => m_layoutPriority;

		// Unity starts measuring this object: anything cached for the axis may describe old content.
		// Heights in particular depend on the width set since the last pass.
		void ILayoutElement.CalculateLayoutInputHorizontal()
		{
			InvalidateMeasure(0);
			OnCalculateLayout(0);
		}

		void ILayoutElement.CalculateLayoutInputVertical()
		{
			InvalidateMeasure(1);
			OnCalculateLayout(1);
		}
	}
}
