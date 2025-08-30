using UnityEngine;

namespace Tekly.DataModels.Binders
{
	/// <summary>
	/// Generically bind a string model to a UnityEvent
	/// </summary>
	public class StringEventBinder : BasicValueBinder<string>
	{
		[SerializeField] private TextSetEvent m_event;
		
		protected override void BindValue(string value)
		{
			m_event?.Invoke(value);
		}
	}
}