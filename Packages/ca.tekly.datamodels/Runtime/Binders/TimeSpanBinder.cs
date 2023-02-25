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

		private int m_lastSeconds = int.MaxValue;
		
		public override void Bind(BinderContainer container)
		{
			if (container.TryGet(m_key.Path, out TimeSpanModel numberValue)) {
				m_disposable?.Dispose();
				m_disposable = numberValue.Subscribe(BindValue);
			}
		}

		private void BindValue(TimeSpan value)
		{
			if (value.Seconds != m_lastSeconds) {
				m_lastSeconds = value.Seconds;
				m_text.text = value.ToString(m_format);	
			}
		}
        
		public override void UnBind()
		{
			base.UnBind();
			m_disposable?.Dispose();
		}
	}
}