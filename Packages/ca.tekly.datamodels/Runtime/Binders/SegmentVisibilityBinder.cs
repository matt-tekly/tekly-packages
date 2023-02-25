using System;
using Tekly.DataModels.Models;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
	public class SegmentVisibilityBinder : Binder
	{
		[SerializeField] private ModelRef m_key;
		[SerializeField] private GameObject[] m_targets;
		
		private IDisposable m_disposable;
        
		public override void Bind(BinderContainer container)
		{
			if (container.TryGet(m_key.Path, out NumberValueModel model)) {
				m_disposable?.Dispose();
				m_disposable = model.Subscribe(BindModel);
			}
		}

		private void BindModel(double value)
		{
			var enabledCount = (int) Math.Floor(value);

			for (var i = 0; i < enabledCount; i++) {
				m_targets[i].SetActive(true);
			}

			for (var i = enabledCount; i < m_targets.Length; i++) {
				m_targets[i].SetActive(false);
			}
		}
        
		public override void UnBind()
		{
			m_disposable?.Dispose();
		}
	}
}