using System;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class ButtonWidget : Widget
	{
		public string Label {
			get => m_label != null ? m_label.Text : null;
			set {
				if (m_label != null) {
					m_label.Text = value;
				}
			}
		}
		
		[SerializeField] private LabelWidget m_label;
		
		private Action m_onActivate;

		public void Initialize(Action onActivate, string label)
		{
			m_onActivate = onActivate;
			
			if (m_label != null) {
				m_label.Text = label;
			}
		}
		
		/// <summary>
		/// Sets what the button does, keeping its label. For buttons that are part of a prefab.
		/// </summary>
		public ButtonWidget WithAction(Action onActivate)
		{
			m_onActivate = onActivate;
			return this;
		}

		public void Activate()
		{
			m_onActivate?.Invoke();
		}
	}
}