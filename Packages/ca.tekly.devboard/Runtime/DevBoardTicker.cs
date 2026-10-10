using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Lives on the DevBoard root and ticks behaviours that aren't inside a panel, every frame, watches for the
	/// show/hide shortcut, and runs scope rebuilds at the end of the frame.
	/// </summary>
	[AddComponentMenu("")]
	public class DevBoardTicker : MonoBehaviour
	{
		internal TickGroup TickGroup { get; } = new();

		private void Update()
		{
			DevBoard.Instance.UpdateKeys();
			TickGroup.Tick();
		}

		private void LateUpdate()
		{
			DevBoard.Instance.RunRebuilds();
		}
	}
}
