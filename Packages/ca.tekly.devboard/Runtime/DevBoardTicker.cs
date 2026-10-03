using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Lives on the DevBoard root and ticks behaviours that aren't inside a Board, every frame.
	/// </summary>
	[AddComponentMenu("")]
	public class DevBoardTicker : MonoBehaviour
	{
		internal TickGroup TickGroup { get; } = new();

		private void Update()
		{
			TickGroup.Tick();
		}
	}
}
