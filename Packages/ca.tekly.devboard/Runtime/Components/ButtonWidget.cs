using System;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class ButtonWidget : Widget
	{
		public string Label {
			get => m_label.Text;
			set => m_label.Text = value;
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
		
		public void Activate()
		{
			m_onActivate?.Invoke();
		}
	}
}