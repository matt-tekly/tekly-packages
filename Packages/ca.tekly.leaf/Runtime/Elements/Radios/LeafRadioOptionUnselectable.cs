using UnityEngine;

namespace Tekly.Leaf.Elements.Radios
{
	/// <summary>
	/// A radio option that never takes keyboard focus, e.g. tabs that switch pages while focus stays in the content.
	/// Clicking asks its <see cref="LeafRadioGroup"/> to turn it on; reach these from the keyboard through
	/// <see cref="LeafRadioGroup.SelectNext"/>. Without a group it toggles on and off by itself.
	/// Needs the Input System UI module, and no Selectable above it in the hierarchy (see
	/// <see cref="LeafButtonUnselectable"/>).
	/// </summary>
	public class LeafRadioOptionUnselectable : LeafButtonUnselectable, ILeafRadioOption
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
			UpdateAnimatorState();

			if (m_membership.ConsumeChange() && notify) {
				m_onValueChanged.Invoke(IsOn);
			}
		}

		protected override void OnClick()
		{
			if (!IsActive() || !IsInteractable()) {
				return;
			}

			m_membership.Press();
			base.OnClick();
		}

		protected override void OnEnable()
		{
			m_membership.Enable(this, this);
			base.OnEnable();
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
