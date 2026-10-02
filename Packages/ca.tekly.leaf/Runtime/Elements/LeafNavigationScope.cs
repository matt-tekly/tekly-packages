using System;
using System.Collections.Generic;
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
		[NonSerialized] private GameObject m_lastEventSystemSelection;
		[NonSerialized] private bool m_needsInitialSelection;

		private static readonly List<Selectable> s_selectables = new();
		private static LeafNavigationScope s_lastActiveScope;

		private void OnEnable()
		{
			m_lastValidSelection = m_firstSelection;
			m_needsInitialSelection = !SelectGameObject();
		}

		private void OnDisable()
		{
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

		public virtual Selectable FindNext(Selectable current, MoveDirection direction)
		{
			var dir = direction switch {
				MoveDirection.Left => Vector3.left,
				MoveDirection.Right => Vector3.right,
				MoveDirection.Up => Vector3.up,
				MoveDirection.Down => Vector3.down,
				_ => Vector3.zero
			};

			if (dir == Vector3.zero) {
				return null;
			}

			var allowWrap = direction is MoveDirection.Left or MoveDirection.Right
				? m_wrapHorizontal
				: m_wrapVertical;

			return FindBest(current, dir, allowWrap);
		}

		/// <summary>
		/// Finds the Selectable after current in hierarchy order, wrapping at the ends. A null current, or one that
		/// isn't navigable, gives the first Selectable, or the last when reversed.
		/// </summary>
		public virtual Selectable FindNextInTabOrder(Selectable current, bool isReverse)
		{
			CollectSelectables(s_selectables);

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

		protected virtual Selectable FindBest(Selectable current, Vector3 direction, bool allowWrap)
		{
			var rectTransform = current.transform as RectTransform;
			var localDir = Quaternion.Inverse(current.transform.rotation) * direction;
			var origin = current.transform.TransformPoint(GetPointOnRectEdge(rectTransform, localDir));

			Selectable bestForward = null;
			var bestForwardPrimary = float.PositiveInfinity;
			var bestForwardSecondary = float.PositiveInfinity;
			var hasForwardCandidate = false;

			Selectable bestWrap = null;
			var bestWrapSecondary = float.PositiveInfinity;
			var bestWrapPrimary = float.NegativeInfinity;

			CollectSelectables(s_selectables);

			for (var i = 0; i < s_selectables.Count; i++) {
				var candidate = s_selectables[i];

				if (!ShouldIncludeInNavigation(current, candidate)) {
					continue;
				}

				var candidateRect = candidate.transform as RectTransform;
				var candidateCenter = candidateRect != null
					? (Vector3) candidateRect.rect.center
					: Vector3.zero;

				var vector = candidate.transform.TransformPoint(candidateCenter) - origin;
				if (vector.sqrMagnitude <= 0.0001f) {
					continue;
				}

				var primary = Vector3.Dot(direction, vector);
				var projected = direction * primary;
				var secondary = (vector - projected).sqrMagnitude;

				if (primary > 0.001f) {
					hasForwardCandidate = true;

					if (primary < bestForwardPrimary - 0.0001f ||
						(Mathf.Abs(primary - bestForwardPrimary) <= 0.0001f && secondary < bestForwardSecondary)) {
						bestForward = candidate;
						bestForwardPrimary = primary;
						bestForwardSecondary = secondary;
					}

					continue;
				}

				if (!allowWrap) {
					continue;
				}

				var wrapPrimary = -primary;
				if (wrapPrimary <= 0.001f) {
					continue;
				}

				if (secondary < bestWrapSecondary - 0.0001f ||
					(Mathf.Abs(secondary - bestWrapSecondary) <= 0.0001f && wrapPrimary > bestWrapPrimary)) {
					bestWrap = candidate;
					bestWrapSecondary = secondary;
					bestWrapPrimary = wrapPrimary;
				}
			}

			s_selectables.Clear();

			if (bestForward != null) {
				return bestForward;
			}

			if (allowWrap && hasForwardCandidate) {
				return bestWrap;
			}

			return null;
		}

		protected virtual bool ShouldIncludeInNavigation(Selectable current, Selectable candidate)
		{
			return candidate != null && candidate != current;
		}

		private void Update()
		{
			var eventSystem = EventSystem.current;
			if (eventSystem == null) {
				return;
			}

			if (m_needsInitialSelection) {
				// Widgets are often added after the scope is enabled, so keep trying until something is selected
				m_needsInitialSelection = eventSystem.currentSelectedGameObject == null && !SelectGameObject();
			}

			HandleTab(eventSystem);

			var currentGo = eventSystem.currentSelectedGameObject;

			if (m_lastEventSystemSelection == currentGo) {
				return;
			}
			m_lastEventSystemSelection = currentGo;
			EventSystemSelectionChanged(m_lastEventSystemSelection);
		}

		private void EventSystemSelectionChanged(GameObject newSelection)
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

		private void HandleTab(EventSystem eventSystem)
		{
			if (!LeafTabInput.WasPressedThisFrame(out var isReverse) || LeafCore.Instance.DisableInput.IsHeld.Value) {
				return;
			}

			var selected = eventSystem.currentSelectedGameObject;

			if (selected == null) {
				// Clicking the background clears the selection, Tab picks up again in the scope used last
				if (s_lastActiveScope == this) {
					SelectGameObject();
				}

				return;
			}

			// Only the nearest scope handles Tab, so nested and sibling scopes don't all move the selection
			if (selected.GetComponentInParent<LeafNavigationScope>() != this) {
				return;
			}

			var eventData = new LeafTabEventData(eventSystem) {
				IsReverse = isReverse
			};

			ExecuteEvents.Execute(selected, eventData, LeafExecuteEvents.TabHandler);

			if (eventData.used) {
				return;
			}

			selected.TryGetComponent(out Selectable current);

			var next = FindNextInTabOrder(current, isReverse);
			if (next != null) {
				eventSystem.SetSelectedGameObject(next.gameObject, eventData);
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

		private static bool IsNavigable(Selectable selectable)
		{
			return selectable.IsActive() &&
			       selectable.IsInteractable() &&
			       selectable.navigation.mode != Navigation.Mode.None;
		}

		private static Vector3 GetPointOnRectEdge(RectTransform rect, Vector2 dir)
		{
			if (rect == null) {
				return Vector3.zero;
			}

			if (dir != Vector2.zero) {
				dir /= Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
			}

			dir = rect.rect.center + Vector2.Scale(rect.rect.size, dir * 0.5f);
			return dir;
		}
	}
}
