using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// Receives Tab and Shift+Tab while selected. <see cref="LeafNavigationScope"/> sends this before doing its own
	/// tab navigation; call Use() on the event data to stop that navigation.
	/// </summary>
	public interface ILeafTabHandler : IEventSystemHandler
	{
		void OnTab(LeafTabEventData eventData);
	}

	public class LeafTabEventData : BaseEventData
	{
		/// <summary>
		/// True for Shift+Tab
		/// </summary>
		public bool IsReverse { get; set; }

		public LeafTabEventData(EventSystem eventSystem) : base(eventSystem) { }
	}

	public static class LeafExecuteEvents
	{
		public static readonly ExecuteEvents.EventFunction<ILeafTabHandler> TabHandler = ExecuteTab;

		private static void ExecuteTab(ILeafTabHandler handler, BaseEventData eventData)
		{
			handler.OnTab(ExecuteEvents.ValidateEventData<LeafTabEventData>(eventData));
		}
	}
}
