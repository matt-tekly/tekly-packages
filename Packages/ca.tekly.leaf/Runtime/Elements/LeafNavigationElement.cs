using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	public class LeafNavigationElement : UIBehaviour
	{
		public LeafNavigationScope Scope => m_scope;
		public Selectable Selectable => m_selectable;
		
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
			       m_selectable.IsInteractable() &&
			       m_selectable.navigation.mode != Navigation.Mode.None;
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