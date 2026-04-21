using Tekly.Leaf.Elements.Animators;
using UnityEngine;

namespace Tekly.Leaf.Elements.Radios
{
	public class LeafRadioOption : LeafButtonUnselectable
	{
		public bool IsOn {
			get => m_isOn;
			set {
				m_isOn = value;
				UpdateAnimatorMode();
			}
		}
		
		[SerializeField] private LeafRadioGroup m_group;
		
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