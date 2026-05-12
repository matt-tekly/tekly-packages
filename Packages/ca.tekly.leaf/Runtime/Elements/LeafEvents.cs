using System;
using UnityEngine.Events;

namespace Tekly.Leaf.Elements
{
	[Serializable]
	public class ButtonClickedEvent : UnityEvent { }
	
	[Serializable]
	public class SelectableSelectedEvent : UnityEvent<bool> {}
}