using Tekly.Leaf.Elements;
using UnityEngine;

namespace Tekly.Leaf
{
	public static class LeafInputExtensions
	{
		/// <summary>
		/// <see cref="LeafCore.IsInputDisabled"/>, unless element is under a <see cref="LeafIgnoreDisableInput"/>.
		/// Leaf elements check this before acting on clicks, submits, drags and navigation.
		/// </summary>
		public static bool IsLeafInputDisabled(this Component element)
		{
			return LeafCore.Instance.IsInputDisabled && !LeafIgnoreDisableInput.Covers(element);
		}
	}
}
