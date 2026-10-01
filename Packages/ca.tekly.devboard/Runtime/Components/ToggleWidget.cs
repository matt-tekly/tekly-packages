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
			m_setValue(isOn);
		}

		public void Initialize(string label, Func<bool> getValue, Action<bool> setValue)
		{
			m_label.Text = label;
			m_getValue = getValue;
			m_setValue = setValue;
			
			m_toggle.SetIsOnWithoutNotify(getValue());
		}

		protected override void Tick()
		{
			m_toggle.isOn = m_getValue();
		}
	}
}