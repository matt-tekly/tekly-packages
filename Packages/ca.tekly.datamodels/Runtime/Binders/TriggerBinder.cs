using System;
using Tekly.Common.Observables;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.DataModels.Binders
{
	public class TriggerBinder : Binder
	{
		public UnityEvent Triggered => m_triggered;
		
		[SerializeField] private ModelRef m_key;
		[SerializeField] private UnityEvent m_triggered;
        
		private IDisposable m_disposable;

		public override void Bind(BinderContainer container)
		{
			if (container.TryGet(m_key.Path, out TriggerModel model)) {
				m_disposable?.Dispose();
				m_disposable = model.Subscribe(BindValue);
			}
		}

		private void BindValue(Unit unit)
		{
			if (m_triggered != null) {
				m_triggered.Invoke();
			}
		}
		
		public override void UnBind()
		{
			m_disposable?.Dispose();
			m_disposable = null;
		}
	}
}