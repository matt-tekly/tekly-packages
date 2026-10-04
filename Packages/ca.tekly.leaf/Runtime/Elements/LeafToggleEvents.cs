using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	public class LeafToggleEvents : MonoBehaviour
	{
		[SerializeField] private LeafToggle m_toggle;
		[SerializeField] private Toggle.ToggleEvent m_events;
		[SerializeField] private Toggle.ToggleEvent m_eventsInverted;
		
		private void Awake()
		{
			m_toggle.onValueChanged.AddListener(OnValueChanged);
		}

		private void OnDestroy()
		{
			m_toggle.onValueChanged.RemoveListener(OnValueChanged);
		}

		private void OnValueChanged(bool isOn)
		{
			m_events.Invoke(isOn);
			m_eventsInverted.Invoke(!isOn);
		}
	}
}