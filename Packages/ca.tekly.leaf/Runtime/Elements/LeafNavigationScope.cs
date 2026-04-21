using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements
{
	public class LeafNavigationScope : MonoBehaviour
	{
		[SerializeField] private bool m_wrapHorizontal = true;
		[SerializeField] private bool m_wrapVertical = true;

		private readonly HashSet<LeafNavigationElement> m_selectables = new();

		private GameObject m_lastSelected;

		private void OnEnable()
		{
			SelectGameObject();
		}

		public void SelectGameObject()
		{
			if (m_lastSelected != null) {
				EventSystem.current.SetSelectedGameObject(m_lastSelected);
			}
		}

		public void Register(LeafNavigationElement navigationElement)
		{
			if (navigationElement == null) {
				return;
			}

			m_selectables.Add(navigationElement);

			m_lastSelected = navigationElement.gameObject;
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

			if (!navigationElement.gameObject.activeInHierarchy) {
				return false;
			}

			return true;
		}

		private void Update()
		{
			var eventSystem = EventSystem.current;
			if (eventSystem == null) {
				return;
			}

			var currentGo = eventSystem.currentSelectedGameObject;
			if (currentGo != null && currentGo != m_lastSelected && currentGo.transform.IsChildOf(transform)) {
				m_lastSelected =  currentGo;
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
