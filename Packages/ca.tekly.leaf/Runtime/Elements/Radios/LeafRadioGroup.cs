using System;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements.Radios
{
	public class LeafRadioGroup : LeafSelectable, ILeafRadioGroup
	{
		[SerializeField] private LayoutAxis m_layoutAxis;
		
		private LeafRadioOption m_currentOption;

		protected override void OnEnable()
		{
			if (m_currentOption == null) {
				var childOption = GetComponentInChildren<LeafRadioOption>();
				if (childOption != null) {
					SetActiveOption(childOption);	
				}
			}

			base.OnEnable();
		}

		private void SetActiveOption(LeafRadioOption childOption)
		{
			TurnOffCurrentOption();
			
			m_currentOption = childOption;
			m_currentOption.SetValueFromGroup(true);
		}

		private void TrySelectNextOption(int modify)
		{
			if (m_currentOption != null) {
				var parent = m_currentOption.transform.parent;
				var siblingIndex = m_currentOption.transform.GetSiblingIndex();
				var nextIndex =  Mathf.Clamp(siblingIndex + modify, 0, parent.childCount - 1);
								
				var nextChild = parent.GetChild(nextIndex);

				if (nextChild.TryGetComponent(out LeafRadioOption nextOption)) {
					SetActiveOption(nextOption);
				}
			} else {
				var childOption = GetComponentInChildren<LeafRadioOption>();
				if (childOption != null) {
					SetActiveOption(childOption);	
				}
			}
		}
		
		public override void OnMove(AxisEventData eventData)
		{
			switch (m_layoutAxis) {
				case LayoutAxis.Horizontal:
					switch (eventData.moveDir) {
						case MoveDirection.Left:
							TrySelectNextOption(-1);
							return;
						case MoveDirection.Right:
							TrySelectNextOption(1);
							return;
						case MoveDirection.Up:
						case MoveDirection.Down:
							base.OnMove(eventData);
							return;
					}
					break;
				case LayoutAxis.Vertical:
					switch (eventData.moveDir) {
						case MoveDirection.Left:
						case MoveDirection.Right:
							base.OnMove(eventData);
							return;
						case MoveDirection.Up:
							TrySelectNextOption(-1);
							return;
						case MoveDirection.Down:
							TrySelectNextOption(1);
							return;
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		
		}

		public void OnOptionPressed(LeafRadioOption option)
		{
			if (TryGetComponent(out LeafRadioGroup _)) {
				if (EventSystem.current == null || EventSystem.current.alreadySelecting) {
					return;
				}

				EventSystem.current.SetSelectedGameObject(gameObject);
			}
			
			TurnOffCurrentOption();

			m_currentOption = option;
			m_currentOption.SetValueFromGroup(true);
		}

		public void OnOptionSetOn(LeafRadioOption option)
		{
			if (option == m_currentOption) {
				return;
			}
			
			TurnOffCurrentOption();
			m_currentOption = option;
		}
		
		private void TurnOffCurrentOption()
		{
			if (m_currentOption != null) {
				m_currentOption.SetValueFromGroup(false);
			}
		}
	}
}