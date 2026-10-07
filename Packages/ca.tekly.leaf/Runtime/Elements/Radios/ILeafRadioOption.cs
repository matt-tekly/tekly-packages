using System;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.Leaf.Elements.Radios
{
	[Serializable]
	public class LeafRadioValueEvent : UnityEvent<bool> { }

	/// <summary>
	/// An option of a <see cref="LeafRadioGroup"/>. The group owns which option is on; an option only shows it.
	/// <see cref="LeafRadioOption"/> takes keyboard focus like any Selectable (radio buttons in a form),
	/// <see cref="LeafRadioOptionUnselectable"/> never does (tabs that switch pages without moving focus).
	/// </summary>
	public interface ILeafRadioOption
	{
		LeafRadioGroup Group { get; }
		bool IsOn { get; set; }
		LeafRadioValueEvent OnValueChanged { get; }

		/// <summary>
		/// Invoked each time the option is pressed, even when it was already on and nothing changed.
		/// Use <see cref="OnValueChanged"/> to react to the option turning on or off.
		/// </summary>
		ButtonClickedEvent OnClicked { get; }

		// Implemented by Component/Behaviour/Selectable already
		Transform transform { get; }
		bool isActiveAndEnabled { get; }
		bool IsInteractable();

		/// <summary>
		/// Called by the group when this option turned on or off, or when the group's interactable changed.
		/// </summary>
		void RefreshState(bool notify);
	}

	/// <summary>
	/// The group bookkeeping shared by both option types: finding the group, telling it when the option is
	/// enabled or disabled, and answering IsOn. An option without a group keeps its own on/off, like a checkbox.
	/// </summary>
	internal sealed class LeafRadioMembership
	{
		public LeafRadioGroup Group { get; private set; }
		public bool GroupAllowsInteraction => Group == null || Group.Interactable;
		public bool IsOn => Group != null ? Group.Current == m_option : m_isOn;

		private ILeafRadioOption m_option;
		private bool m_isOn;
		private bool m_wasOn;

		public void Enable(ILeafRadioOption option, Component component)
		{
			m_option = option;
			Group = component.GetComponentInParent<LeafRadioGroup>();

			if (Group != null) {
				Group.OnOptionEnabled(option);
			}

			m_wasOn = IsOn;
		}

		public void Disable()
		{
			// Group is kept so IsOn still answers for the group's current option
			if (Group != null && m_option != null) {
				Group.OnOptionDisabled(m_option);
			}
		}

		public void ParentChanged(Behaviour behaviour)
		{
			if (!behaviour.isActiveAndEnabled || m_option == null) {
				return;
			}

			// Moving along with the group, e.g. an ancestor of both was reparented, changes nothing. Leaving and
			// rejoining would make the group pick another option, since this one is disabled for a moment.
			if (behaviour.GetComponentInParent<LeafRadioGroup>() == Group) {
				return;
			}

			Disable();
			Enable(m_option, behaviour);
			m_option.RefreshState(true);
		}

		public void SetIsOn(bool value)
		{
			if (Group != null) {
				if (value) {
					Group.Select(m_option);
				} else if (Group.Current == m_option) {
					Group.Select(null);
				}

				return;
			}

			m_isOn = value;
			m_option?.RefreshState(true);
		}

		public void Press()
		{
			if (Group != null) {
				Group.OnOptionPressed(m_option);
			} else {
				SetIsOn(!m_isOn);
			}
		}

		/// <summary>
		/// True once each time IsOn has changed since the last call.
		/// </summary>
		public bool ConsumeChange()
		{
			var isOn = IsOn;
			if (isOn == m_wasOn) {
				return false;
			}

			m_wasOn = isOn;
			return true;
		}
	}
}
