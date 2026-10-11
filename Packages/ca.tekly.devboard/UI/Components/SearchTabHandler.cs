using System;
using Tekly.Leaf.Elements;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// Added to a popup's search field so Tab and Shift+Tab move through the results instead of leaving the field.
	/// Leaf sends Tab to every handler on the selected object before doing its own tab navigation, and skips that
	/// navigation once one of them uses the event.
	/// </summary>
	[AddComponentMenu("")]
	internal class SearchTabHandler : MonoBehaviour, ILeafTabHandler
	{
		/// <summary>
		/// Called with true for Shift+Tab.
		/// </summary>
		public Action<bool> Tabbed;

		public void OnTab(LeafTabEventData eventData)
		{
			if (Tabbed == null) {
				return;
			}

			Tabbed(eventData.IsReverse);
			eventData.Use();
		}
	}
}
