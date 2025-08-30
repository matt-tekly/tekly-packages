using System;
using Tekly.DataModels.Models;
using TMPro;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
	public class TimeSpanBinder : BasicBinder<TimeSpanModel>
	{
		[SerializeField] private TMP_Text m_text;
		[SerializeField] private string m_format = "mm':'ss";
		
		private DateTime m_endTime;

		private double m_lastSeconds = double.MaxValue;
		
		protected override IDisposable Subscribe(TimeSpanModel model)
		{
			return model.Subscribe(BindValue);
		}

		private void BindValue(TimeSpan value)
		{
			if (Math.Abs(value.TotalSeconds - m_lastSeconds) > 0.01d) {
				m_lastSeconds = value.TotalSeconds;
				m_text.text = value.ToString(m_format);	
			}
		}
		
#if UNITY_EDITOR
		private void OnValidate()
		{
			if (m_text == null) {
				m_text = GetComponent<TMP_Text>();
			}
		}
#endif
	}
}