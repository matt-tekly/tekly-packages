#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#elif ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine;
#endif

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// Reads the Tab key. The UI input modules never send Tab, so <see cref="LeafNavigationScope"/> polls it here.
	/// </summary>
	public static class LeafTabInput
	{
		/// <summary>
		/// True on the frame Tab is pressed. isReverse is true when Shift is held.
		/// </summary>
		public static bool WasPressedThisFrame(out bool isReverse)
		{
#if ENABLE_INPUT_SYSTEM
			var keyboard = Keyboard.current;
			if (keyboard == null) {
				isReverse = false;
				return false;
			}

			isReverse = keyboard.shiftKey.isPressed;
			return keyboard.tabKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
			isReverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			return Input.GetKeyDown(KeyCode.Tab);
#else
			isReverse = false;
			return false;
#endif
		}
	}
}
