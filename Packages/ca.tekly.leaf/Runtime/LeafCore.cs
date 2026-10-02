using System;
using Tekly.Common.LifeCycles;
using Tekly.Common.Utils;
using Tekly.Leaf.Elements;

namespace Tekly.Leaf
{
	public class LeafCore : Singleton<LeafCore>, IDisposable
	{
		public readonly Latch DisableInput = new();
		public readonly LeafSelection Selection = new();

		public LeafCore()
		{
			LifeCycle.Instance.LateUpdate += OnLateUpdate;
		}

		public IDisposable DisableInputScope(object owner)
		{
			return DisableInput.HoldScope(owner);
		}

		public void Dispose()
		{
			LifeCycle.Instance.LateUpdate -= OnLateUpdate;
		}

		private void OnLateUpdate()
		{
			// Tab first, so subscribers hear about the selection it moved to in the same frame
			LeafNavigationScope.ProcessTab();
			Selection.Refresh();
		}
	}
}
