using UnityEngine;

namespace Tekly.Leaf.Elements.Animators
{
	public enum LeafElementMode
	{
		Normal,
		Highlighted,
		Pressed,
		Selected,
		Disabled,
	}
	
	public abstract class LeafAnimator : MonoBehaviour
	{
		public abstract void HandleMode(LeafElementMode mode, bool on, bool instant);
	}
}