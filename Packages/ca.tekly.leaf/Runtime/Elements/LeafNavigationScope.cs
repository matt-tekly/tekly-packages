using System;
using System.Collections.Generic;
using Tekly.Leaf.Elements.Radios;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// Keeps keyboard and gamepad navigation inside part of the hierarchy. Every active, interactable Selectable
	/// below the scope takes part, except those inside a nested scope and those whose Navigation mode is None.
	/// Arrow keys move spatially (<see cref="FindNext"/>); Tab and Shift+Tab follow hierarchy order
	/// (<see cref="FindNextInTabOrder"/>).
	/// </summary>
	public class LeafNavigationScope : MonoBehaviour
	{
		public LeafElementSelectedEvent OnSelected
		{
			get => m_onSelected;
			set => m_onSelected = value;
		}

		[Serializable]
		public class LeafElementSelectedEvent : UnityEvent<GameObject> {}

		[SerializeField] private LeafElementSelectedEvent m_onSelected = new();
		[SerializeField] private bool m_wrapHorizontal = true;
		[SerializeField] private bool m_wrapVertical = true;
		[SerializeField] private GameObject m_firstSelection;

		[NonSerialized] private GameObject m_lastValidSelection;
		[NonSerialized] private GameObject m_lastSelection;
		[NonSerialized] private bool m_needsInitialSelection;

		private IDisposable m_selectionSubscription;

		private static readonly List<Selectable> s_selectables = new();
		private static readonly Vector3[] s_corners = new Vector3[4];
		private static LeafNavigationScope s_lastActiveScope;

		private void OnEnable()
		{
			m_lastValidSelection = m_firstSelection;
			m_selectionSubscription = LeafCore.Instance.Selection.Current.Subscribe(OnSelectionChanged);
			m_needsInitialSelection = !SelectGameObject();
		}

		private void OnDisable()
		{
			m_selectionSubscription?.Dispose();
			m_selectionSubscription = null;

			if (s_lastActiveScope == this) {
				s_lastActiveScope = null;
			}
		}

		/// <summary>
		/// Selects the last selection made in this scope, then the first selection, then the first Selectable in
		/// tab order. Returns false when there was nothing to select.
		/// </summary>
		public bool SelectGameObject()
		{
			var eventSystem = EventSystem.current;
			if (eventSystem == null) {
				return false;
			}

			GameObject target = null;

			if (m_lastValidSelection != null && m_lastValidSelection.activeInHierarchy) {
				target = m_lastValidSelection;
			} else if (m_firstSelection != null && m_firstSelection.activeInHierarchy) {
				target = m_firstSelection;
			} else {
				var first = FindNextInTabOrder(null, false);
				if (first != null) {
					target = first.gameObject;
				}
			}

			if (target == null) {
				return false;
			}

			eventSystem.SetSelectedGameObject(target);
			return true;
		}

		/// <summary>
		/// For a Selectable's OnMove: navigates within the nearest scope above it, if there is one.
		/// </summary>
		public static bool TryNavigateFrom(Selectable current, AxisEventData eventData)
		{
			var scope = current.GetComponentInParent<LeafNavigationScope>();
			return scope != null && scope.isActiveAndEnabled && scope.TryNavigate(current, eventData);
		}

		/// <summary>
		/// Selects the Selectable after current in tab order within the nearest scope above it, as if Tab
		/// were pressed. Returns false when there's no scope or nothing else to select.
		/// </summary>
		public static bool TrySelectNextFrom(Selectable current, bool isReverse)
		{
			var scope = current.GetComponentInParent<LeafNavigationScope>();
			return scope != null && scope.isActiveAndEnabled && scope.TrySelectNextInTabOrder(current, isReverse);
		}

		/// <summary>
		/// Selects the Selectable after current in tab order. Returns false when there's nothing else to select.
		/// </summary>
		public bool TrySelectNextInTabOrder(Selectable current, bool isReverse, BaseEventData eventData = null)
		{
			var eventSystem = EventSystem.current;
			if (eventSystem == null) {
				return false;
			}

			var next = FindNextInTabOrder(current, isReverse);
			if (next == null) {
				return false;
			}

			eventSystem.SetSelectedGameObject(next.gameObject, eventData);
			return true;
		}

		/// <summary>
		/// Moves the selection from current in the event's direction. Returns false when there's nowhere to go.
		/// </summary>
		public bool TryNavigate(Selectable current, AxisEventData eventData)
		{
			var next = FindNext(current, eventData.moveDir);
			if (next == null) {
				return false;
			}

			EventSystem.current.SetSelectedGameObject(next.gameObject, eventData);
			return true;
		}

		/// <summary>
		/// Finds the Selectable to move to from current in a direction, wrapping around the scope when nothing
		/// is that way and wrapping is on for that axis.
		/// </summary>
		public virtual Selectable FindNext(Selectable current, MoveDirection direction)
		{
			if (direction == MoveDirection.None) {
				return null;
			}

			var allowWrap = direction is MoveDirection.Left or MoveDirection.Right
				? m_wrapHorizontal
				: m_wrapVertical;

			return FindBest(current, direction, allowWrap);
		}

		/// <summary>
		/// Finds the Selectable after current in hierarchy order, wrapping at the ends. A null current, or one that
		/// isn't navigable, gives the first Selectable, or the last when reversed.
		/// </summary>
		public virtual Selectable FindNextInTabOrder(Selectable current, bool isReverse)
		{
			CollectSelectables(s_selectables);
			RemoveSkippedTabStops(current, s_selectables);

			var count = s_selectables.Count;
			var step = isReverse ? -1 : 1;
			var index = current != null ? s_selectables.IndexOf(current) : -1;

			if (index < 0) {
				index = isReverse ? count : -1;
			}

			Selectable next = null;

			for (var i = 1; i <= count; i++) {
				var candidate = s_selectables[((index + step * i) % count + count) % count];

				if (candidate != current) {
					next = candidate;
					break;
				}
			}

			s_selectables.Clear();
			return next;
		}

		/// <summary>
		/// Fills output with the Selectables that take part in this scope's navigation, in hierarchy order.
		/// </summary>
		protected virtual void CollectSelectables(List<Selectable> output)
		{
			output.Clear();
			CollectChildren(transform, output);
		}

		/// <summary>
		/// Spatial search based on Android's FocusFinder, comparing whole rects in the scope's space. A candidate
		/// has to be past current in the direction, not just have a center a little further along, so a
		/// neighbour in the same row never counts as "below". Candidates that line up with current (overlap it
		/// across the direction) beat ones that don't, and the rest are scored by edge gap, weighted well above
		/// the sideways offset.
		/// </summary>
		protected virtual Selectable FindBest(Selectable current, MoveDirection direction, bool allowWrap)
		{
			CollectSelectables(s_selectables);

			var source = GetNavRect(current.transform, direction);
			var best = FindBestFrom(current, source, direction);

			if (best == null && allowWrap) {
				// Search again as if current sat just before everything else, e.g. Right from the end of a row
				// finds the start of that row
				var start = source.Near;

				for (var i = 0; i < s_selectables.Count; i++) {
					start = Mathf.Min(start, GetNavRect(s_selectables[i].transform, direction).Near);
				}

				best = FindBestFrom(current, source.MovedBefore(start), direction);
			}

			s_selectables.Clear();
			return RedirectIntoRadioGroup(current, best);
		}

		protected virtual bool ShouldIncludeInNavigation(Selectable current, Selectable candidate)
		{
			return candidate != null && candidate != current;
		}

		private void Update()
		{
			if (!m_needsInitialSelection) {
				return;
			}

			var eventSystem = EventSystem.current;
			if (eventSystem == null) {
				return;
			}

			// Widgets are often added after the scope is enabled, so keep trying until something is selected
			m_needsInitialSelection = eventSystem.currentSelectedGameObject == null && !SelectGameObject();
		}

		private void OnSelectionChanged(GameObject newSelection)
		{
			var isChild = newSelection != null && newSelection.transform.IsChildOf(transform);

			if (isChild) {
				m_lastValidSelection = newSelection;

				if (newSelection.GetComponentInParent<LeafNavigationScope>() == this) {
					s_lastActiveScope = this;
				}
			}

			if (m_lastSelection != newSelection) {
				m_lastSelection = isChild ? newSelection : null;
				m_onSelected?.Invoke(m_lastSelection);
			}
		}

		/// <summary>
		/// Reads Tab and passes it to the nearest scope above the selection. Called by <see cref="LeafCore"/>
		/// every LateUpdate, since the UI input modules never send Tab.
		/// </summary>
		internal static void ProcessTab()
		{
			if (!LeafTabInput.WasPressedThisFrame(out var isReverse) || LeafCore.Instance.DisableInput.IsHeld.Value) {
				return;
			}

			var eventSystem = EventSystem.current;
			if (eventSystem == null) {
				return;
			}

			var selected = eventSystem.currentSelectedGameObject;

			if (selected == null) {
				// Clicking the background clears the selection, Tab picks up again in the scope used last
				if (s_lastActiveScope != null) {
					s_lastActiveScope.SelectGameObject();
				}

				return;
			}

			var scope = selected.GetComponentInParent<LeafNavigationScope>();
			if (scope != null && scope.isActiveAndEnabled) {
				scope.HandleTab(eventSystem, selected, isReverse);
			}
		}

		protected virtual void HandleTab(EventSystem eventSystem, GameObject selected, bool isReverse)
		{
			var eventData = new LeafTabEventData(eventSystem) {
				IsReverse = isReverse
			};

			ExecuteEvents.Execute(selected, eventData, LeafExecuteEvents.TabHandler);

			if (eventData.used) {
				return;
			}

			selected.TryGetComponent(out Selectable current);
			TrySelectNextInTabOrder(current, isReverse, eventData);
		}

		/// <summary>
		/// Moving into a radio group from outside lands on its current option, not just the nearest one.
		/// </summary>
		private static Selectable RedirectIntoRadioGroup(Selectable current, Selectable target)
		{
			if (target == null || !target.TryGetComponent(out ILeafRadioOption option) || option.Group == null) {
				return target;
			}

			if (current != null && current.TryGetComponent(out ILeafRadioOption currentOption) && currentOption.Group == option.Group) {
				return target;
			}

			var entry = option.Group.GetEntrySelectable();
			return entry != null ? entry : target;
		}

		/// <summary>
		/// A radio group that's a single tab stop keeps only its entry option in the tab order, and none of its
		/// options while focus is already inside it, so Tab leaves the group. current always stays, so stepping
		/// from it still works.
		/// </summary>
		private static void RemoveSkippedTabStops(Selectable current, List<Selectable> selectables)
		{
			LeafRadioGroup currentGroup = null;
			if (current != null && current.TryGetComponent(out ILeafRadioOption currentOption)) {
				currentGroup = currentOption.Group;
			}

			for (var i = selectables.Count - 1; i >= 0; i--) {
				var selectable = selectables[i];

				if (selectable == current || !selectable.TryGetComponent(out ILeafRadioOption option)) {
					continue;
				}

				var group = option.Group;
				if (group == null || !group.SingleTabStop) {
					continue;
				}

				if (group == currentGroup || selectable != group.GetEntrySelectable()) {
					selectables.RemoveAt(i);
				}
			}
		}

		private static void CollectChildren(Transform parent, List<Selectable> output)
		{
			for (var i = 0; i < parent.childCount; i++) {
				var child = parent.GetChild(i);

				if (!child.gameObject.activeInHierarchy) {
					continue;
				}

				// A nested scope keeps its Selectables to itself
				if (child.TryGetComponent(out LeafNavigationScope _)) {
					continue;
				}

				if (child.TryGetComponent(out Selectable selectable) && IsNavigable(selectable)) {
					output.Add(selectable);
				}

				CollectChildren(child, output);
			}
		}

		internal static bool IsNavigable(Selectable selectable)
		{
			return selectable.IsActive() &&
			       selectable.IsInteractable() &&
			       selectable.navigation.mode != Navigation.Mode.None;
		}

		private Selectable FindBestFrom(Selectable current, NavRect source, MoveDirection direction)
		{
			Selectable best = null;
			var bestRect = default(NavRect);

			for (var i = 0; i < s_selectables.Count; i++) {
				var candidate = s_selectables[i];

				if (!ShouldIncludeInNavigation(current, candidate)) {
					continue;
				}

				var rect = GetNavRect(candidate.transform, direction);

				if (!source.IsCandidate(rect)) {
					continue;
				}

				if (best == null || IsBetter(source, rect, bestRect, direction)) {
					best = candidate;
					bestRect = rect;
				}
			}

			return best;
		}

		/// <summary>
		/// The target's bounds in this scope's space, rotated so the direction always points along +Major.
		/// </summary>
		private NavRect GetNavRect(Transform target, MoveDirection direction)
		{
			float xMin, xMax, yMin, yMax;

			if (target is RectTransform rectTransform) {
				rectTransform.GetWorldCorners(s_corners);

				xMin = yMin = float.PositiveInfinity;
				xMax = yMax = float.NegativeInfinity;

				for (var i = 0; i < s_corners.Length; i++) {
					var point = transform.InverseTransformPoint(s_corners[i]);
					xMin = Mathf.Min(xMin, point.x);
					xMax = Mathf.Max(xMax, point.x);
					yMin = Mathf.Min(yMin, point.y);
					yMax = Mathf.Max(yMax, point.y);
				}
			} else {
				var point = transform.InverseTransformPoint(target.position);
				xMin = xMax = point.x;
				yMin = yMax = point.y;
			}

			return direction switch {
				MoveDirection.Right => new NavRect(xMin, xMax, yMin, yMax),
				MoveDirection.Left => new NavRect(-xMax, -xMin, yMin, yMax),
				MoveDirection.Up => new NavRect(yMin, yMax, xMin, xMax),
				_ => new NavRect(-yMax, -yMin, xMin, xMax)
			};
		}

		private static bool IsBetter(NavRect source, NavRect rect, NavRect best, MoveDirection direction)
		{
			if (BeamBeats(source, rect, best, direction)) {
				return true;
			}

			if (BeamBeats(source, best, rect, direction)) {
				return false;
			}

			return source.WeightedDistance(rect) < source.WeightedDistance(best);
		}

		/// <summary>
		/// Whether a, which lines up with source, beats b, which doesn't.
		/// </summary>
		private static bool BeamBeats(NavRect source, NavRect a, NavRect b, MoveDirection direction)
		{
			if (!source.BeamOverlaps(a) || source.BeamOverlaps(b)) {
				return false;
			}

			// b overlaps source along the direction, so it's barely that way at all
			if (!source.IsFullyBefore(b)) {
				return true;
			}

			// Sideways, staying in the row always wins. Up and down, an out of line element that's much
			// closer can still win, e.g. a short row of buttons directly under a wide panel
			if (direction is MoveDirection.Left or MoveDirection.Right) {
				return true;
			}

			return source.Gap(a) < source.GapToFarEdge(b);
		}

		/// <summary>
		/// A rect seen from the direction of travel: Near/Far run along it, Min/Max across it.
		/// </summary>
		private readonly struct NavRect
		{
			private const float GAP_WEIGHT = 13f;

			public readonly float Near;
			public readonly float Far;
			public readonly float Min;
			public readonly float Max;

			public NavRect(float near, float far, float min, float max)
			{
				Near = near;
				Far = far;
				Min = min;
				Max = max;
			}

			public NavRect MovedBefore(float position)
			{
				var shift = position - 1f - Far;
				return new NavRect(Near + shift, Far + shift, Min, Max);
			}

			/// <summary>
			/// Further along than this rect: starts after it (or ends past it while starting past its start).
			/// </summary>
			public bool IsCandidate(NavRect other)
			{
				return (Near < other.Near || Far <= other.Near) && Far < other.Far;
			}

			public bool BeamOverlaps(NavRect other)
			{
				return Min < other.Max && other.Min < Max;
			}

			public bool IsFullyBefore(NavRect other)
			{
				return Far <= other.Near;
			}

			public float Gap(NavRect other)
			{
				return Mathf.Max(0f, other.Near - Far);
			}

			public float GapToFarEdge(NavRect other)
			{
				return Mathf.Max(1f, other.Far - Far);
			}

			public float WeightedDistance(NavRect other)
			{
				var gap = Gap(other);
				var offset = (Min + Max) * 0.5f - (other.Min + other.Max) * 0.5f;
				return GAP_WEIGHT * gap * gap + offset * offset;
			}
		}
	}
}
