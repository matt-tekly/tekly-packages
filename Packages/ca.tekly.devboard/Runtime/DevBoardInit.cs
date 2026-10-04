#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace Tekly.DevBoard
{
	[DefaultExecutionOrder(-9000)]
	public class DevBoardInit : MonoBehaviour
	{
		[SerializeField] private DevBoardAssets m_assets;

		[Tooltip("Create the main panel when there are no panels, e.g. on first launch. Panels saved from the last session are restored either way")]
		[SerializeField] private bool m_createMainPanel = true;

#if ENABLE_INPUT_SYSTEM
		[Tooltip("Shows and hides DevBoard. None turns the shortcut off")]
		[SerializeField] private Key m_toggleKey = Key.Backquote;
#else
		[Tooltip("Shows and hides DevBoard. None turns the shortcut off")]
		[SerializeField] private KeyCode m_toggleKey = KeyCode.BackQuote;
#endif

		[Tooltip("Start with DevBoard hidden until the shortcut is pressed")]
		[SerializeField] private bool m_startHidden = true;

		private void Awake()
		{
			var devBoard = DevBoard.Instance;
			devBoard.ToggleKey = m_toggleKey;
			devBoard.SetVisible(!m_startHidden);
			devBoard.AddAssets(m_assets);
			devBoard.RestorePanels();

			if (m_createMainPanel && devBoard.Panels.Count == 0) {
				devBoard.Panel(DevBoard.MAIN_PANEL_ID);
			}
		}
	}
}
