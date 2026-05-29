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
				if (m_isOn != value) {
					m_isOn = value;
					UpdateAnimatorMode();
					
					if (m_isOn) {
						var group = GetComponentInParent<ILeafRadioGroup>(true);
			
						if (group != null) {
							group.OnOptionSetOn(this);	
						}	
					}
					
					m_onValueChanged.Invoke(m_isOn);
				}
			}
		}
		
		public RadioEvent OnValueChanged => m_onValueChanged;
		[SerializeField] private RadioEvent m_onValueChanged = new RadioEvent();
		
		private bool m_isOn;

		public void SetValueFromGroup(bool isOn)
		{
			m_isOn = isOn;
			UpdateAnimatorMode();
				
			m_onValueChanged.Invoke(m_isOn);
		}
		
		protected override void OnClick()
		{
			var group = GetComponentInParent<ILeafRadioGroup>();
			
			if (group != null) {
				group.OnOptionPressed(this);	
			}
			
			base.OnClick();
		}
		
		protected override void UpdateAnimatorMode(LeafElementMode mode, bool instant)
		{
			if (m_animator != null) {
				m_animator.HandleMode(mode, m_isOn, instant);
			}
		}
	}
}