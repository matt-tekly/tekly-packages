using System;
using UnityEngine.Events;

namespace Tekly.DataModels.Binders
{
	[Serializable]
	public class TextSetEvent : UnityEvent<string> {}
}