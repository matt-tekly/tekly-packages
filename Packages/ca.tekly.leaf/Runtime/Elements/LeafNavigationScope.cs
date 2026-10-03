using System;
using System.Collections.Generic;
using Tekly.Leaf.Elements.Radios;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// Where focus lands in a <see cref="LeafNavigationScope"/> when it's enabled, or when navigation enters it
	/// from outside.
	/// </summary>
	public enum LeafScopeEntry
	{
		/// First Selection, or the first Selectable in tab order.
		First,
		/// The Selectable last focused in the scope if it's still there, otherwise like First.
		Remembered,
		/// Arrows land wherever the spatial search picks, e.g. the same row of a panel beside this one. Tab lands
		/// on the first Selectable (the last with Shift+Tab). When enabled, like First.
		Nearest
	}

	/// <summary>
	/// Keyboard and gamepad navigation for part of the hierarchy. Every active, interactable Selectable below the
	/// scope takes part, except those whose Navigation mode is None. Arrow keys move spatially
	/// (<see cref="FindNext"/>); Tab and Shift+Tab follow hierarchy order (<see cref="FindNextInTabOrder"/>).
	///
	/// With Contain Navigation on, the scope is a boundary: navigation stays inside it, and scopes above don't
	/// see its Selectables. With it off, the scope only decides where focus lands (<see cref="LeafScopeEntry"/>):
	/// navigation is handled by the nearest containing scope above, which moves in and out of it freely, e.g.
	/// tab panels or side by side columns inside a board.
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

		/// <summary>
		/// When false, the nearest containing scope above handles navigation and this scope only decides where
		/// focus lands.
		/// </summary>
		public bool ContainNavigation => m_containNavigation;
		public LeafScopeEntry Entry => m_entry;

		[SerializeField] private LeafElementSelectedEvent m_onSelected = new();

		[Tooltip("On: arrows and Tab stay inside this scope. Off: the containing scope above handles navigation and can move in and out of this one; this scope only decides where focus lands")]
		[SerializeField] private bool m_containNavigation = true;

		[Tooltip("Where focus lands when this scope is enabled, or when navigation enters it from outside")]
		[SerializeField] private LeafScopeEntry m_entry = LeafScopeEntry.First;

		[Tooltip("Select the entry when this scope is enabled. Turn off when something else decides when it takes focus, e.g. LeafTabPanels")]
		[SerializeField] private bool m_selectOnEnable = true;

		[Tooltip("On: Left and Right only move to Selectables in the same row (overlapping vertically), wrapping within that row. Off: when the row has nothing that way, they can move diagonally to the nearest Selectable")]
		[SerializeField] private bool m_sidewaysStaysInRow = true;

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
			if (m_entry != LeafScopeEntry.Remembered) {
				m_lastValidSelection = null;
			}

			m_selectionSubscription = LeafCore.Instance.Selection.Current.Subscribe(OnSelectionChanged);
			m_needsInitialSelection = m_selectOnEnable && ShouldSelectOnEnable() && !SelectGameObject();
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
		/// Selects where focus lands in this scope (see <see cref="Entry"/>). Returns false when there was nothing
		/// to select.
		/// </summary>
		public bool SelectGameObject()
		{
			return SelectGameObject(false);
		}

		/// <summary>
		/// Selects where focus lands in this scope now, or once something selectable shows up, e.g. a tab panel
		/// that was just swapped to. Stops waiting if something visible gets focus first. Does nothing while the
		/// scope is disabled.
		/// </summary>
		public void TakeFocus()
		{
			if (!isActiveAndEnabled) {
				return;
			}

			m_needsInitialSelection = !SelectGameObject();
		}

		/// <summary>
		/// The GameObject focus lands on in this scope: the remembered selection (for Remembered, or when
		/// preferLast), then First Selection, then the first Selectable in tab order.
		/// </summary>
		public GameObject GetEntry(bool preferLast = false)
		{
			if ((preferLast || m_entry == LeafScopeEntry.Remembered) && IsUsable(m_lastValidSelection)) {
				return m_lastValidSelection;
			}

			if (m_firstSelection != null && m_firstSelection.activeInHierarchy) {
				return m_firstSelection;
			}

			var first = FindNextInTabOrder(null, false);
			return first != null ? first.gameObject : null;
		}

		/// <summary>
		/// The scope that handles navigation for target: the nearest enabled scope above it that contains
		/// navigation, or the nearest enabled scope when none does.
		/// </summary>
		public static LeafNavigationScope FindNavigationScope(Transform target)
		{
			LeafNavigationScope nearest = null;

			for (var current = target; current != null; current = current.parent) {
				if (!current.TryGetComponent(out LeafNavigationScope scope) || !scope.isActiveAndEnabled) {
					continue;
				}

				if (scope.m_containNavigation) {
					return scope;
				}

				nearest ??= scope;
			}

			return nearest;
		}

		/// <summary>
		/// For a Selectable's OnMove: navigates within the scope that handles navigation for it, if there is one.
		/// </summary>
		public static bool TryNavigateFrom(Selectable current, AxisEventData eventData)
		{
			var scope = FindNavigationScope(current.transform);
			return scope != null && scope.TryNavigate(current, eventData);
		}

		/// <summary>
		/// Selects the Selectable after current in tab order, as if Tab were pressed. Returns false when there's
		/// no scope or nothing else to select.
		/// </summary>
		public static bool TrySelectNextFrom(Selectable current, bool isReverse)
		{
			var scope = FindNavigationScope(current.transform);
			return scope != null && scope.TrySelectNextInTabOrder(current, isReverse);
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
			return RedirectIntoScope(current, next, true);
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

			var isSideways = direction is MoveDirection.Left or MoveDirection.Right;
			var source = GetNavRect(current.transform, direction);
			var best = FindBestFrom(current, source, direction, isSideways && m_sidewaysStaysInRow);

			if (best == null && allowWrap) {
				// Search again as if current sat just before everything else. Sideways only the same row counts,
				// so Right from the end of a row finds the start of that row rather than whatever sits nearest
				// the scope's left edge
				var start = source.Near;

				for (var i = 0; i < s_selectables.Count; i++) {
					start = Mathf.Min(start, GetNavRect(s_selectables[i].transform, direction).Near);
				}

				best = FindBestFrom(current, source.MovedBefore(start), direction, isSideways);
			}

			s_selectables.Clear();
			return RedirectIntoRadioGroup(current, RedirectIntoScope(current, best, false));
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

			// Widgets are often added after the scope is enabled, so keep trying until something is selected.
			// A hidden selection counts as none, e.g. a control in the tab panel that was just switched away from
			var selected = eventSystem.currentSelectedGameObject;
			m_needsInitialSelection = (selected == null || !selected.activeInHierarchy) && !SelectGameObject();
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

			// Clicking the background clears the selection, and hiding a panel leaves it on a hidden object.
			// Either way Tab picks up again where it was in the scope used last
			if (selected == null || !selected.activeInHierarchy) {
				if (s_lastActiveScope != null) {
					s_lastActiveScope.SelectGameObject(true);
				}

				return;
			}

			var scope = FindNavigationScope(selected.transform);
			if (scope != null) {
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

		private bool SelectGameObject(bool preferLast)
		{
			var eventSystem = EventSystem.current;
			if (eventSystem == null) {
				return false;
			}

			var target = GetEntry(preferLast);
			if (target == null) {
				return false;
			}

			eventSystem.SetSelectedGameObject(target);
			return true;
		}

		/// <summary>
		/// A containing scope takes focus whenever it's enabled. One that doesn't contain navigation only does when
		/// nothing visible is focused in the scope that navigates it, e.g. a tab panel replacing the one that had
		/// focus, but not a column shown at the same time as the board around it.
		/// </summary>
		private bool ShouldSelectOnEnable()
		{
			if (m_containNavigation) {
				return true;
			}

			var eventSystem = EventSystem.current;
			var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;

			if (selected == null || !selected.activeInHierarchy) {
				return true;
			}

			var navigationScope = FindNavigationScope(transform);
			return navigationScope == null || !selected.transform.IsChildOf(navigationScope.transform);
		}

		/// <summary>
		/// Moving into a scope that doesn't contain navigation, from outside it, lands on that scope's entry
		/// (unless its entry is Nearest). Nested scopes are entered from the outermost. Tab already enters at the
		/// first (or last) Selectable in order, so it's only redirected to a remembered selection.
		/// </summary>
		private Selectable RedirectIntoScope(Selectable current, Selectable target, bool isTab)
		{
			if (target == null) {
				return null;
			}

			var path = ListPool<LeafNavigationScope>.Get();

			for (var t = target.transform; t != null && t != transform; t = t.parent) {
				if (t.TryGetComponent(out LeafNavigationScope scope) && scope.isActiveAndEnabled && !scope.m_containNavigation) {
					path.Add(scope);
				}
			}

			var result = target;

			for (var i = path.Count - 1; i >= 0; i--) {
				var scope = path[i];

				if (current != null && current.transform.IsChildOf(scope.transform)) {
					continue;
				}

				if (scope.m_entry == LeafScopeEntry.Nearest) {
					continue;
				}

				if (isTab && (scope.m_entry != LeafScopeEntry.Remembered || !IsUsable(scope.m_lastValidSelection))) {
					continue;
				}

				var entry = scope.GetEntry();
				if (entry != null && entry.TryGetComponent(out Selectable selectable) && IsNavigable(selectable)) {
					result = selectable;
				}

				break;
			}

			ListPool<LeafNavigationScope>.Release(path);
			return result;
		}

		private static LeafRadioGroup GetRadioGroup(Selectable selectable)
		{
			return selectable != null && selectable.TryGetComponent(out ILeafRadioOption option) ? option.Group : null;
		}

		private static bool IsUsable(GameObject target)
		{
			return target != null && target.activeInHierarchy &&
			       target.TryGetComponent(out Selectable selectable) && IsNavigable(selectable);
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

				// A nested scope that contains navigation keeps its Selectables to itself
				if (child.TryGetComponent(out LeafNavigationScope nested) && nested.enabled && nested.m_containNavigation) {
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

		private Selectable FindBestFrom(Selectable current, NavRect source, MoveDirection direction, bool requireBeam)
		{
			Selectable best = null;
			var bestRect = default(NavRect);
			var currentGroup = GetRadioGroup(current);

			for (var i = 0; i < s_selectables.Count; i++) {
				var candidate = s_selectables[i];

				if (!ShouldIncludeInNavigation(current, candidate)) {
					continue;
				}

				// A radio group moves between its own options (LeafRadioGroup.TryMove), so a move it turned down,
				// e.g. at an end without wrapping, mustn't land back inside it by the scope wrapping around the row
				if (currentGroup != null && GetRadioGroup(candidate) == currentGroup) {
					continue;
				}

				var rect = GetNavRect(candidate.transform, direction);

				if (!source.IsCandidate(rect) || (requireBeam && !source.BeamOverlaps(rect))) {
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
