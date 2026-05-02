using System;
using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.Leaf.Elements.Radios
{
	public class LeafRadioOption : LeafButtonUnselectable
	{
		[Serializable]
		public class RadioEvent : UnityEvent<bool> {}
		
		public bool IsOn {
			get => m_isOn;
			set {
				m_isOn = value;
				UpdateAnimatorMode();
				
				m_onValueChanged.Invoke(m_isOn);
			}
		}
		
		public RadioEvent OnValueChanged => m_onValueChanged;
		
		[SerializeField] private LeafRadioGroup m_group;
		[SerializeField] private RadioEvent m_onValueChanged = new RadioEvent();
		
		private bool m_isOn;
		
		protected override void OnClick()
		{
			m_group.OnOptionClicked(this);
			base.OnClick();
		}
		
		protected override void UpdateAnimatorMode(LeafElementMode mode, bool instant)
		{
			if (m_animator != null) {
				m_animator.HandleMode(mode, IsOn, instant);
			}
		}
	}
}