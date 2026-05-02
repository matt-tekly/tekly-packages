using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements.Radios
{
	// TODO: This needs a way to not be selectable, or I need to make something like tabs
	public class LeafRadioGroup : LeafSelectable
	{
		[SerializeField] private LeafRadioOption[] m_options;
		[SerializeField] private bool m_selectOnOptionClicked = true;

		private LeafRadioOption m_currentOption;

		protected override void OnEnable()
		{
			if (m_currentOption == null) {
				SetActiveOption(0);
			}

			base.OnEnable();
		}

		public override void OnMove(AxisEventData eventData)
		{
			switch (eventData.moveDir) {
				case MoveDirection.Left:
					if (m_currentOption == null) {
						SetActiveOption(0);
					} else {
						var currentIndex = Array.IndexOf(m_options, m_currentOption);
						SetActiveOption(Mathf.Clamp(currentIndex - 1, 0, m_options.Length - 1));
					}

					return;
				case MoveDirection.Right:
					if (m_currentOption == null) {
						SetActiveOption(0);
					} else {
						var currentIndex = Array.IndexOf(m_options, m_currentOption);
						SetActiveOption(Mathf.Clamp(currentIndex + 1, 0, m_options.Length - 1));
					}

					return;
				case MoveDirection.Up:
				case MoveDirection.Down:
					base.OnMove(eventData);
					return;
			}
		}

		public void SetActiveOption(int index)
		{
			TurnOffCurrentOption();
			m_currentOption = m_options[index];
			m_currentOption.IsOn = true;
		}

		public void OnOptionClicked(LeafRadioOption option)
		{
			if (m_selectOnOptionClicked) {
				Select();	
			}
			
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