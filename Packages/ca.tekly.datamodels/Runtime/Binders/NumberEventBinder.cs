using UnityEngine;
using UnityEngine.Events;

namespace Tekly.DataModels.Binders
{
	/// <summary>
	/// Generically bind a number model to a UnityEvent
	/// </summary>
	public class NumberEventBinder : BasicValueBinder<double>
	{
		[SerializeField] private UnityEvent<float> m_event;

		protected override void BindValue(double value)
		{
			m_event?.Invoke((float)value);
		}
	}
}