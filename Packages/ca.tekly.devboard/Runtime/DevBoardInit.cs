using UnityEngine;

namespace Tekly.DevBoard
{
	[DefaultExecutionOrder(-9000)]
	public class DevBoardInit : MonoBehaviour
	{
		[SerializeField] private DevBoardAssets m_assets;

		[Tooltip("Create the main panel if it doesn't exist yet. Panels saved from the last session are restored either way")]
		[SerializeField] private bool m_createMainPanel = true;

		private void Awake()
		{
			var devBoard = DevBoard.Instance;
			devBoard.AddAssets(m_assets);
			devBoard.RestorePanels();

			if (m_createMainPanel) {
				devBoard.Panel(DevBoard.MAIN_PANEL_ID);
			}
		}
	}
}
