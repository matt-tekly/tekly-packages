using System;
using Tekly.Common.Utils;
using UnityEngine.EventSystems;

namespace Tekly.Leaf
{
	public class LeafCore : Singleton<LeafCore>
	{
		public readonly Latch DisableInput = new();
		
		public IDisposable DisableInputScope(object owner)
		{
			return DisableInput.HoldScope(owner);
		}
	}
}