using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements.Radios
{
	public class LeafRadioGroupUnselectable : UIBehaviour, ILeafRadioGroup
	{
		public UnityEvent<LeafRadioOption> OptionSelected => m_optionSelected;
		public UnityEvent<bool> HasOptionSelected => m_hasOptionSelected;
		public bool IsOptionSelected => m_currentOption != null;
		
		[SerializeField] private bool _allowNoOption;
		[SerializeField] private UnityEvent<LeafRadioOption> m_optionSelected;
		[SerializeField] private UnityEvent<bool> m_hasOptionSelected;
		
		private LeafRadioOption m_currentOption;

		protected override void OnEnable()
		{
			if (m_currentOption == null && !_allowNoOption) {
				var childOption = GetComponentInChildren<LeafRadioOption>();
				if (childOption != null) {
					OnOptionPressed(childOption);	
				}
			}

			base.OnEnable();
		}

		public void OnOptionPressed(LeafRadioOption option)
		{
			if (option == m_currentOption) {
				if (_allowNoOption) {
					m_currentOption.SetValueFromGroup(false);
					m_currentOption = null;
					
					OptionSelected?.Invoke(null);
					HasOptionSelected?.Invoke(false);
				}
				
				return;
			}
			        
			TurnOffCurrentOption();

			m_currentOption = option;
			m_currentOption.SetValueFromGroup(true);
			
			OptionSelected?.Invoke(option);
			HasOptionSelected?.Invoke(true);
		}

		public void OnOptionSetOn(LeafRadioOption option)
		{
			if (option == m_currentOption) {
				return;
			}
			
			TurnOffCurrentOption();

			m_currentOption = option;
			OptionSelected?.Invoke(option);
			HasOptionSelected?.Invoke(true);
		}

		private void TurnOffCurrentOption()
		{
			if (m_currentOption != null) {
				m_currentOption.SetValueFromGroup(false);
			}
		}
	}
}