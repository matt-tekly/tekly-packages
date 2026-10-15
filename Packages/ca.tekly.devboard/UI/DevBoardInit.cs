#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Tekly.DevBoard.BuiltIn;
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

		[Tooltip("Shows and hides DevBoard when this many fingers touch the screen together, e.g. 4 on mobile. 0 turns the gesture off")]
		[SerializeField, Min(0)] private int m_toggleTouchCount;

		[Tooltip("Start with DevBoard hidden until the shortcut is pressed")]
		[SerializeField] private bool m_startHidden = true;

		[Tooltip("Add DevBoard's own pages, like System/Performance")]
		[SerializeField] private bool m_builtInPages = true;

		private void Awake()
		{
			var devBoard = DevBoardUI.Instance;
			devBoard.ToggleKey = m_toggleKey;
			devBoard.ToggleTouchCount = m_toggleTouchCount;
			devBoard.SetVisible(!m_startHidden);
			devBoard.AddAssets(m_assets);

			if (m_builtInPages) {
				BuiltInPages.Register(gameObject);
			}

			devBoard.RestorePanels();

			if (m_createMainPanel && devBoard.Panels.Count == 0) {
				devBoard.Panel(DevBoardUI.MAIN_PANEL_ID);
			}
		}
	}
}
