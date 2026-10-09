using UnityEngine;

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// Leaf elements on or below this keep taking input while <see cref="LeafCore.DisableInput"/> is held, e.g. for
	/// debug UI that has to work during transitions and tutorials. Only the nearest one above an element counts, so
	/// disabling it turns the override off for everything below it.
	/// </summary>
	[DisallowMultipleComponent]
	public class LeafIgnoreDisableInput : MonoBehaviour
	{
		public static bool Covers(Component element)
		{
			var marker = element.GetComponentInParent<LeafIgnoreDisableInput>();
			return marker != null && marker.isActiveAndEnabled;
		}
	}
}
