#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#else
using UnityEngine;
#endif

namespace Tekly.DevBoard
{
	/// <summary>
	/// The few keys DevBoard's popups listen to, read from whichever input system the project uses.
	/// </summary>
	internal static class KeyInput
	{
#if ENABLE_INPUT_SYSTEM
		public static bool Escape => WasPressed(Keyboard.current?.escapeKey);
		public static bool Up => WasPressed(Keyboard.current?.upArrowKey);
		public static bool Down => WasPressed(Keyboard.current?.downArrowKey);
		public static bool Enter => WasPressed(Keyboard.current?.enterKey) || WasPressed(Keyboard.current?.numpadEnterKey);
		public static bool Shift => Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

		private static bool WasPressed(UnityEngine.InputSystem.Controls.KeyControl key)
		{
			return key != null && key.wasPressedThisFrame;
		}
#elif ENABLE_LEGACY_INPUT_MANAGER
		public static bool Escape => Input.GetKeyDown(KeyCode.Escape);
		public static bool Up => Input.GetKeyDown(KeyCode.UpArrow);
		public static bool Down => Input.GetKeyDown(KeyCode.DownArrow);
		public static bool Enter => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
		public static bool Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
		public static bool Escape => false;
		public static bool Up => false;
		public static bool Down => false;
		public static bool Enter => false;
		public static bool Shift => false;
#endif
	}
}
