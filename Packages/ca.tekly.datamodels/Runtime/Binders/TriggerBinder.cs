using System;
using Tekly.Common.Observables;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.DataModels.Binders
{
	public class TriggerBinder : BasicBinder<TriggerModel>
	{
		public UnityEvent Triggered => m_triggered;
		
		[SerializeField] private UnityEvent m_triggered;
		[SerializeField] private bool m_activateTriggerOnBind;
		
		protected override IDisposable Subscribe(TriggerModel model)
		{
			if (m_activateTriggerOnBind) {
				model.Emit();
			}
			
			return model.Subscribe(BindValue);
		}

		private void BindValue(Unit unit)
		{
			if (m_triggered != null) {
				m_triggered.Invoke();
			}
		}

		public void ActivateTrigger()
		{
			m_model?.Emit();
		}
	}
}