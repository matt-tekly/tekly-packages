using Tekly.DataModels.Binders;
using Tekly.Leaf.Elements.Radios;
using Tekly.Logging;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.Leaf.Binders
{
	/// <summary>
	/// Binds a bool model to a radio option of either kind (<see cref="LeafRadioOption"/> or <see cref="LeafRadioOptionUnselectable"/>).
	/// </summary>
	public class LeafRadioOptionBinder : BasicValueBinder<bool>
	{
		[Tooltip("A LeafRadioOption or LeafRadioOptionUnselectable")]
		[FormerlySerializedAs("m_radioOption")]
		[SerializeField] private MonoBehaviour m_option;
		[SerializeField] private bool m_ignoreModelUpdates;

		private ILeafRadioOption m_radioOption;
		private bool m_hasBoundValue;

		private void Awake()
		{
			m_radioOption = m_option as ILeafRadioOption;

			if (m_radioOption != null) {
				m_radioOption.OnValueChanged.AddListener(OnValueChanged);
			} else {
				TkLogger.Get<LeafRadioOptionBinder>().ErrorContext("Needs a LeafRadioOption or LeafRadioOptionUnselectable", this);
			}
		}

		protected override void BindValue(bool value)
		{
			if (m_radioOption == null || (m_ignoreModelUpdates && m_hasBoundValue)) {
				return;
			}

			m_hasBoundValue = true;
			m_radioOption.IsOn = value;
		}

		private void OnValueChanged(bool isOn)
		{
			if (m_model != null) {
				m_model.Value = isOn;
			} else {
				TkLogger.Get<LeafRadioOptionBinder>().ErrorContext("Changed without a model", this);
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (m_option == null || m_option is not ILeafRadioOption) {
				m_option = GetComponent<ILeafRadioOption>() as MonoBehaviour;
			}
		}
#endif
	}
}
