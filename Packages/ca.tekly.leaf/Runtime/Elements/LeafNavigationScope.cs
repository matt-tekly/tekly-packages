using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements
{
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

		private readonly HashSet<LeafNavigationElement> m_selectables = new();

		[NonSerialized] private GameObject m_lastValidSelection;
		[NonSerialized] private GameObject m_lastSelection;
		[NonSerialized] private GameObject m_lastEventSystemSelection;

		private static readonly List<LeafNavigationElement> s_tabOrder = new();
		private static LeafNavigationScope s_lastActiveScope;

		private void OnEnable()
		{
			m_lastValidSelection = m_firstSelection;
			SelectGameObject();
		}

		private void OnDisable()
		{
			if (s_lastActiveScope == this) {
				s_lastActiveScope = null;
			}
		}

		public void SelectGameObject()
		{
			if (m_lastValidSelection != null) {
				EventSystem.current.SetSelectedGameObject(m_lastValidSelection);
			} else if (m_firstSelection != null) {
				EventSystem.current.SetSelectedGameObject(m_firstSelection);
			}
		}

		public void Register(LeafNavigationElement navigationElement)
		{
			if (navigationElement == null) {
				return;
			}

			m_selectables.Add(navigationElement);

			if (m_lastValidSelection == null) {
				m_lastValidSelection = navigationElement.gameObject;	
			}
			
			if (EventSystem.current.currentSelectedGameObject == null) {
				SelectGameObject();
			}
		}

		public void Unregister(LeafNavigationElement navigationElement)
		{
			if (navigationElement == null) {
				return;
			}

			m_selectables.Remove(navigationElement);

			if (m_lastValidSelection == navigationElement.gameObject) {
				m_lastValidSelection = null;
				// TODO: Should we try to find a next valid selection?
			}
		}

		public virtual LeafNavigationElement FindNext(LeafNavigationElement current, MoveDirection direction)
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
		/// Finds the element after current in tab order, wrapping at the ends. Elements are in hierarchy order,
		/// except that each <see cref="LeafNavigationGroup"/> is one block sorted by its own order.
		/// A null current gives the first element, or the last when reversed.
		/// </summary>
		public virtual LeafNavigationElement FindNextInTabOrder(LeafNavigationElement current, bool isReverse)
		{
			LeafTabOrderBuilder.Build(this, s_tabOrder);

			var count = s_tabOrder.Count;
			var step = isReverse ? -1 : 1;
			var index = current != null ? s_tabOrder.IndexOf(current) : -1;

			if (index < 0) {
				index = isReverse ? count : -1;
			}

			LeafNavigationElement next = null;

			for (var i = 1; i <= count; i++) {
				var candidate = s_tabOrder[((index + step * i) % count + count) % count];

				if (candidate != current && candidate.IsTabCandidate()) {
					next = candidate;
					break;
				}
			}

			s_tabOrder.Clear();
			return next;
		}

		protected virtual LeafNavigationElement FindBest(LeafNavigationElement current, Vector3 direction, bool allowWrap)
		{
			var rectTransform = current.transform as RectTransform;
			var localDir = Quaternion.Inverse(current.transform.rotation) * direction;
			var origin = current.transform.TransformPoint(GetPointOnRectEdge(rectTransform, localDir));

			LeafNavigationElement bestForward = null;
			var bestForwardPrimary = float.PositiveInfinity;
			var bestForwardSecondary = float.PositiveInfinity;
			var hasForwardCandidate = false;

			LeafNavigationElement bestWrap = null;
			var bestWrapSecondary = float.PositiveInfinity;
			var bestWrapPrimary = float.NegativeInfinity;

			foreach (var selectable in m_selectables) {
				if (!ShouldIncludeInNavigation(current, selectable)) {
					continue;
				}

				var selectableRect = selectable.transform as RectTransform;
				var selectableCenter = selectableRect != null
					? (Vector3) selectableRect.rect.center
					: Vector3.zero;

				var vector = selectable.transform.TransformPoint(selectableCenter) - origin;
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
						bestForward = selectable;
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
					bestWrap = selectable;
					bestWrapSecondary = secondary;
					bestWrapPrimary = wrapPrimary;
				}
			}

			if (bestForward != null) {
				return bestForward;
			}

			if (allowWrap && hasForwardCandidate) {
				return bestWrap;
			}

			return null;
		}

		protected virtual bool ShouldIncludeInNavigation(LeafNavigationElement current, LeafNavigationElement navigationElement)
		{
			if (navigationElement == null || navigationElement == current) {
				return false;
			}

			return navigationElement.IsNavigationCandidate();
		}

		private void Update()
		{
			var eventSystem = EventSystem.current;
			if (eventSystem == null) {
				return;
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

			// A selected object without a LeafNavigationElement isn't in the tab order, so Tab starts from the top
			selected.TryGetComponent(out LeafNavigationElement current);

			var next = FindNextInTabOrder(current, isReverse);
			if (next != null && next.Selectable != null) {
				eventSystem.SetSelectedGameObject(next.Selectable.gameObject, eventData);
			}
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
