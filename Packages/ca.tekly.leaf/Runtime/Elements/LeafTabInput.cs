using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tekly.Leaf.Elements
{
	/// <summary>
	/// Reads the keys the UI input modules don't send: Tab, which <see cref="LeafNavigationScope"/> polls here, and
	/// the modifier that turns Backspace and Delete into word deletion in <see cref="LeafInputField"/>.
	/// </summary>
	public static class LeafTabInput
	{
		/// <summary>
		/// True while the modifier that deletes whole words is held: Ctrl, or Option on Apple platforms, where
		/// that's the native key for it.
		/// </summary>
		public static bool IsWordModifierHeld()
		{
			var isApple = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX;

#if ENABLE_INPUT_SYSTEM
			var keyboard = Keyboard.current;
			if (keyboard == null) {
				return false;
			}

			return isApple ? keyboard.altKey.isPressed : keyboard.ctrlKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
			return isApple
				? Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)
				: Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#else
			return false;
#endif
		}

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
