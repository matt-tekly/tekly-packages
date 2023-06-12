using System;
using Tekly.DataModels.Models;
using TMPro;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
	public class TimeSpanBinder : Binder
	{
		[SerializeField] private ModelRef m_key;
		[SerializeField] private TMP_Text m_text;
		[SerializeField] private string m_format = "mm':'ss";
        
		private IDisposable m_disposable;
		private DateTime m_endTime;

		private double m_lastSeconds = double.MaxValue;
		
		public override void Bind(BinderContainer container)
		{
			if (container.TryGet(m_key.Path, out TimeSpanModel numberValue)) {
				m_disposable?.Dispose();
				m_disposable = numberValue.Subscribe(BindValue);
			}
		}

		private void BindValue(TimeSpan value)
		{
			if (Math.Abs(value.TotalSeconds - m_lastSeconds) > 0.01d) {
				m_lastSeconds = value.TotalSeconds;
				m_text.text = value.ToString(m_format);	
			}
		}
        
		public override void UnBind()
		{
			base.UnBind();
			m_disposable?.Dispose();
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