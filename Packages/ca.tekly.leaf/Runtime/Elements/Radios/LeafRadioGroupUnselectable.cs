using Tekly.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Tekly.Leaf.Elements.Radios
{
	public class LeafRadioGroupUnselectable : UIBehaviour, ILeafRadioGroup
	{
		public UnityEvent<LeafRadioOption> OptionSelected => m_optionSelected;
		public UnityEvent<bool> HasOptionSelected => m_hasOptionSelected;
		public bool IsOptionSelected => m_currentOption != null;

		[FormerlySerializedAs("_allowNoOption")]
		[SerializeField] private bool m_allowNoOption;
		[SerializeField] private UnityEvent<LeafRadioOption> m_optionSelected;
		[SerializeField] private UnityEvent<bool> m_hasOptionSelected;

		private LeafRadioOption m_currentOption;

		private static readonly TkLogger s_logger = TkLogger.Get<LeafRadioGroupUnselectable>();

		public void ClearCurrentOption()
		{
			if (!m_allowNoOption) {
				s_logger.Error("Trying to clear current option for Radio Group that doesn't allow no current option");
				return;
			}

			ClearOption();
		}

		/// <summary>
		/// Turns option on and the current one off. Null clears the option, if the group allows no option.
		/// </summary>
		public void SelectOption(LeafRadioOption option)
		{
			if (option == m_currentOption) {
				return;
			}

			if (option == null) {
				ClearCurrentOption();
				return;
			}

			SetCurrentOption(option);
		}

		public void OnOptionPressed(LeafRadioOption option)
		{
			if (option == m_currentOption) {
				if (m_allowNoOption) {
					ClearOption();
				}

				return;
			}

			SetCurrentOption(option);
		}

		public void OnOptionSetOn(LeafRadioOption option)
		{
			if (option == m_currentOption) {
				return;
			}

			TurnOffCurrentOption();

			m_currentOption = option;
			m_optionSelected?.Invoke(option);
			m_hasOptionSelected?.Invoke(true);
		}

		public void OnOptionSetOff(LeafRadioOption option)
		{
			if (option != m_currentOption) {
				return;
			}

			m_currentOption = null;
			m_optionSelected?.Invoke(null);
			m_hasOptionSelected?.Invoke(false);
		}

		protected override void OnEnable()
		{
			if (m_currentOption == null && !m_allowNoOption) {
				var childOption = GetComponentInChildren<LeafRadioOption>();
				if (childOption != null) {
					SetCurrentOption(childOption);
				}
			}

			base.OnEnable();
		}

		private void SetCurrentOption(LeafRadioOption option)
		{
			TurnOffCurrentOption();

			m_currentOption = option;
			m_currentOption.SetValueFromGroup(true);

			m_optionSelected?.Invoke(option);
			m_hasOptionSelected?.Invoke(true);
		}

		private void TurnOffCurrentOption()
		{
			if (m_currentOption != null) {
				m_currentOption.SetValueFromGroup(false);
			}
		}

		private void ClearOption()
		{
			TurnOffCurrentOption();
			m_currentOption = null;

			m_optionSelected?.Invoke(null);
			m_hasOptionSelected?.Invoke(false);
		}
	}
}
