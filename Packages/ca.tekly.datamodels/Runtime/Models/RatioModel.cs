using System;
using Tekly.Common.Maths;

namespace Tekly.DataModels.Models
{
	public class RatioModel : ObjectModel
	{
		public double Ratio => m_ratio.Value;
		
		public double Current {
			get => m_current.Value;
			set {
				if (MathUtils.IsApproximately(m_current.Value, value)) {
					return;
				}
				
				m_current.Value = value;
				m_ratio.Value = m_current.Value / m_max.Value;
				
				m_full.Value = m_ratio.Value >= 1.0f;
				EmitModified();
			}
		}

		public double Max {
			get => m_max.Value;
			set {
				if (MathUtils.IsApproximately(m_max.Value, value)) {
					return;
				}
				
				m_max.Value = value;
				m_ratio.Value = m_current.Value / m_max.Value;

				m_full.Value = m_ratio.Value >= 1.0f;
				EmitModified();
			}
		}
		
		public IDisposable SubscribeToRatio(Action<double> action)
		{
			return m_ratio.Subscribe(action);
		}
		
		public IDisposable SubscribeToCurrent(Action<double> action)
		{
			return m_current.Subscribe(action);
		}
		
		public IDisposable SubscribeToMax(Action<double> action)
		{
			return m_max.Subscribe(action);
		}
		
		public IDisposable SubscribeToFull(Action<bool> action)
		{
			return m_full.Subscribe(action);
		}

		public bool IsFull => m_full.Value; 

		private readonly NumberValueModel m_current = new NumberValueModel(1);
		private readonly NumberValueModel m_max = new NumberValueModel(1);
		
		private readonly NumberValueModel m_ratio = new NumberValueModel(1);
		private readonly BoolValueModel m_full = new BoolValueModel(false);

		public RatioModel()
		{
			Add("current", m_current);
			Add("max", m_max);
			Add("ratio", m_ratio);
			Add("full", m_full);
		}
	}
}