using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements.Radios
{
	public class LeafRadioGroupUnselectable : UIBehaviour, ILeafRadioGroup
	{
		[SerializeField] private LeafRadioOption[] m_options;
		private LeafRadioOption m_currentOption;

		protected override void OnEnable()
		{
			if (m_currentOption == null) {
				SetActiveOption(0);
			}

			base.OnEnable();
		}
		
		public void SetActiveOption(int index)
		{
			TurnOffCurrentOption();
			m_currentOption = m_options[index];
			m_currentOption.IsOn = true;
		}

		public void OnOptionPressed(LeafRadioOption option)
		{
			TurnOffCurrentOption();

			m_currentOption = option;
			m_currentOption.IsOn = true;
		}

		private void TurnOffCurrentOption()
		{
			if (m_currentOption != null) {
				m_currentOption.IsOn = false;
			}
		}
	}
}