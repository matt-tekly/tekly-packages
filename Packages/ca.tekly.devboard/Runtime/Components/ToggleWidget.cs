using System;
using Tekly.Leaf.Elements;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class ToggleWidget : Widget
	{
		[SerializeField] private LeafToggle m_toggle;
		[SerializeField] private LabelWidget m_label;

		private Action<bool> m_setValue;
		private Func<bool> m_getValue;

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
			// Only user clicks get here: refreshes use SetIsOnWithoutNotify
			m_setValue?.Invoke(isOn);
		}

		public void Initialize(string label, Func<bool> getValue, Action<bool> setValue)
		{
			m_label.Text = label;
			m_getValue = getValue;
			m_setValue = setValue;

			try {
				m_toggle.SetIsOnWithoutNotify(getValue());
			} catch (Exception) {
				// Don't break the code building the board. Tick retries, and logs the error if it keeps throwing
			}
		}

		protected override void Tick()
		{
			if (m_getValue == null) {
				return;
			}

			var value = m_getValue();

			// Without notify, so a change made by the game isn't pushed back through the setter
			if (m_toggle.isOn != value) {
				m_toggle.SetIsOnWithoutNotify(value);
			}
		}
	}
}
