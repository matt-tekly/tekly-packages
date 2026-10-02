using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// Represents an object that can be navigated to and selected
	/// </summary>
	public class LeafNavigationElement : UIBehaviour
	{
		public LeafNavigationScope Scope => m_scope;
		public Selectable Selectable => m_selectable;

		public bool IsTabStop {
			get => m_isTabStop;
			set => m_isTabStop = value;
		}

		[Tooltip("Include this element in the Tab order. When off, Tab skips it but arrow keys can still select it.")]
		[SerializeField] private bool m_isTabStop = true;

		private LeafNavigationScope m_scope;
		private Selectable m_selectable;

		protected override void Awake()
		{
			base.Awake();
			m_selectable = GetComponent<Selectable>();
			m_scope = GetComponentInParent<LeafNavigationScope>();
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (m_scope == null) {
				m_scope = GetComponentInParent<LeafNavigationScope>();
			}

			m_scope?.Register(this);
		}

		protected override void OnDisable()
		{
			m_scope?.Unregister(this);
			base.OnDisable();
		}

		protected override void OnTransformParentChanged()
		{
			base.OnTransformParentChanged();

			m_scope?.Unregister(this);
			m_scope = GetComponentInParent<LeafNavigationScope>();
			m_scope?.Register(this);
		}

		public bool IsNavigationCandidate()
		{
			return m_selectable != null &&
			       m_selectable.gameObject.activeInHierarchy &&
			       m_selectable.IsInteractable();
		}

		public bool IsTabCandidate()
		{
			return m_isTabStop && IsNavigationCandidate();
		}

		public bool TryNavigate(AxisEventData eventData)
		{
			if (m_scope == null || m_selectable == null) {
				return false;
			}

			var next = m_scope.FindNext(this, eventData.moveDir);
			if (next == null || next.Selectable == null) {
				return false;
			}

			EventSystem.current.SetSelectedGameObject(next.Selectable.gameObject, eventData);
			return true;
		}
	}
}
