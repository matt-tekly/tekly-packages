using System;
using Tekly.Common.Utils;
using UnityEngine.EventSystems;

namespace Tekly.Leaf
{
	public class LeafCore : Singleton<LeafCore>, IDisposable
	{
		private readonly Latch m_eventSystemDisabled = new Latch();
		private readonly IDisposable m_disposable;

		public EventSystem EventSystem => m_eventSystem ??= EventSystem.current;

		private EventSystem m_eventSystem;
		
		public LeafCore()
		{
			m_disposable = m_eventSystemDisabled.IsHeld.SubscribeChanges(disabled => {
				EventSystem.enabled = !disabled;
			});
		}
		
		public IDisposable DisableEventSystemScope(object owner)
		{
			return m_eventSystemDisabled.HoldScope(owner);
		}

		public void Dispose()
		{
			m_disposable?.Dispose();
		}
	}
}