using Tekly.Common.Observables;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.Leaf
{
	/// <summary>
	/// The EventSystem's selected GameObject, tracked in one place. Unity doesn't tell parents when a child is
	/// selected, so anything that cares about selection below it (a scroll view, a navigation scope) subscribes
	/// here instead of polling. <see cref="LeafCore"/> refreshes it every LateUpdate, after the EventSystem has
	/// processed input.
	/// </summary>
	public class LeafSelection
	{
		public IObservableValue<GameObject> Current => m_current;

		private readonly ObservableValue<GameObject> m_current = new();

		/// <summary>
		/// Reads the EventSystem's selection and notifies subscribers if it changed. Call it after changing the
		/// selection to notify right away instead of at the next LateUpdate.
		/// </summary>
		public void Refresh()
		{
			var eventSystem = EventSystem.current;
			m_current.Value = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
		}
	}
}
