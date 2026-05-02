using Tekly.DataModels.Binders;
using Tekly.Leaf.Elements.Radios;
using Tekly.Logging;
using UnityEngine;

namespace TeklySample.Samples.Leaf
{
	public class LeafRadioOptionBinder : BasicValueBinder<bool>
	{
		[SerializeField] private LeafRadioOption m_radioOption;
		[SerializeField] private bool m_ignoreModelUpdates;

		private bool m_hasBoundValue;
		
		private void Awake()
		{
			if (m_radioOption != null) {
				m_radioOption.OnValueChanged.AddListener(OnValueChanged);
			} else {
				TkLogger.Get<LeafRadioOptionBinder>().ErrorContext("Has a null LeafRadioOption", this);
			}
		}

		protected override void BindValue(bool value)
		{
			if (m_ignoreModelUpdates && m_hasBoundValue) {
				return;
			}
			
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
			if (m_radioOption == null) {
				m_radioOption = GetComponent<LeafRadioOption>();
			}
		}
#endif
		
	}
}