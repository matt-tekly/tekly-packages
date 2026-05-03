using System;
using Tekly.Common.Maths;
using Tekly.Common.Observables;

namespace Tekly.DataModels.Models
{
	public class RangeModel : ObjectModel, IObservableValue<double>
	{
		public double CurrentRatio {
			get => MathUtils.InverseLerp(m_min.Value, m_max.Value, m_current.Value);
			set => m_current.Value = MathUtils.Lerp(m_min.Value, m_max.Value, value);
		}

		public double Value => m_current.Value;

		public NumberValueModel Current => m_current;
		public NumberValueModel Min => m_min;
		public NumberValueModel Max => m_max;

		private readonly NumberValueModel m_current;
		private readonly NumberValueModel m_min;
		private readonly NumberValueModel m_max;

		public RangeModel(double value, double min, double max)
		{
			m_current = Add("value", value);
			m_min = Add("min", min);
			m_max = Add("max", max);
		}

		public IDisposable Subscribe(IValueObserver<double> observer)
		{
			return m_current.Subscribe(observer);
		}

		public IDisposable Subscribe(Action<double> observer)
		{
			return m_current.Subscribe(observer);
		}

		public IDisposable SubscribeChanges(Action<double> observer)
		{
			return m_current.SubscribeChanges(observer);
		}
	}
}