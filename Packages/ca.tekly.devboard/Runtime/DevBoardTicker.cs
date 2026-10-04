using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Lives on the DevBoard root and ticks behaviours that aren't inside a panel, every frame, and
	/// watches for the show/hide shortcut.
	/// </summary>
	[AddComponentMenu("")]
	public class DevBoardTicker : MonoBehaviour
	{
		internal TickGroup TickGroup { get; } = new();

		private void Update()
		{
			DevBoard.Instance.CheckToggleKey();
			TickGroup.Tick();
		}
	}
}
