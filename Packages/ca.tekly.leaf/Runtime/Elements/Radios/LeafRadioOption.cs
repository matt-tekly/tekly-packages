using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements.Radios
{
	/// <summary>
	/// A radio option that takes keyboard focus like any Selectable, for choices in a form. Click or Submit asks its
	/// <see cref="LeafRadioGroup"/> to turn it on; the group's settings decide how arrow keys and Tab behave.
	/// Without a group it toggles on and off by itself.
	/// </summary>
	public class LeafRadioOption : LeafSelectable, IPointerClickHandler, ISubmitHandler, ILeafRadioOption
	{
		public LeafRadioGroup Group => m_membership.Group;
		public LeafRadioValueEvent OnValueChanged => m_onValueChanged;

		public bool IsOn {
			get => m_membership.IsOn;
			set => m_membership.SetIsOn(value);
		}

		[SerializeField] private LeafRadioValueEvent m_onValueChanged = new();

		private readonly LeafRadioMembership m_membership = new();

		protected override bool IsOnState => m_membership.IsOn;

		public override bool IsInteractable()
		{
			return base.IsInteractable() && m_membership.GroupAllowsInteraction;
		}

		public void RefreshState(bool notify)
		{
			DoStateTransition(currentSelectionState, false);

			if (m_membership.ConsumeChange() && notify) {
				m_onValueChanged.Invoke(IsOn);
			}
		}

		public virtual void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left) {
				return;
			}

			Press();
		}

		public virtual void OnSubmit(BaseEventData eventData)
		{
			Press();
		}

		private void Press()
		{
			if (!IsActive() || !IsInteractable()) {
				return;
			}

			m_membership.Press();
		}

		public override void OnMove(AxisEventData eventData)
		{
			var group = m_membership.Group;
			if (group != null && group.TryMove(this, eventData.moveDir)) {
				return;
			}

			base.OnMove(eventData);
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			m_membership.Enable(this, this);
			DoStateTransition(currentSelectionState, true);
		}

		protected override void OnDisable()
		{
			m_membership.Disable();
			base.OnDisable();
		}

		protected override void OnTransformParentChanged()
		{
			base.OnTransformParentChanged();
			m_membership.ParentChanged(this);
		}
	}
}
