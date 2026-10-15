using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tekly.DevBoard
{
	/// <summary>
	/// The few keys DevBoard's popups listen to, where a click or touch began, and how many fingers are down,
	/// read from whichever input system the project uses.
	/// </summary>
	internal static class KeyInput
	{
#if ENABLE_INPUT_SYSTEM
		public static bool Escape => WasPressed(Keyboard.current?.escapeKey);
		public static bool Up => WasPressed(Keyboard.current?.upArrowKey);
		public static bool Down => WasPressed(Keyboard.current?.downArrowKey);
		public static bool Enter => WasPressed(Keyboard.current?.enterKey) || WasPressed(Keyboard.current?.numpadEnterKey);
		public static bool Shift => Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

		/// <summary>
		/// Up or Down held, including the frame it's pressed.
		/// </summary>
		public static bool UpOrDownHeld => Keyboard.current != null &&
			(Keyboard.current.upArrowKey.isPressed || Keyboard.current.downArrowKey.isPressed);

		/// <summary>
		/// Ctrl+F, or Cmd+F on a Mac, pressed this frame.
		/// </summary>
		public static bool Find {
			get {
				var keyboard = Keyboard.current;
				return keyboard != null && keyboard.fKey.wasPressedThisFrame &&
					(keyboard.ctrlKey.isPressed || keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed);
			}
		}

		/// <summary>
		/// Where a click or touch began this frame, in screen pixels.
		/// </summary>
		public static bool TryGetPress(out Vector2 position)
		{
			var pointer = Pointer.current;

			if (pointer != null && pointer.press.wasPressedThisFrame) {
				position = pointer.position.ReadValue();
				return true;
			}

			position = default;
			return false;
		}

		/// <summary>
		/// How many fingers are touching the screen. 0 without a touchscreen.
		/// </summary>
		public static int TouchCount {
			get {
				var touchscreen = Touchscreen.current;

				if (touchscreen == null) {
					return 0;
				}

				var touches = touchscreen.touches;
				var count = 0;

				for (var i = 0; i < touches.Count; i++) {
					if (touches[i].press.isPressed) {
						count++;
					}
				}

				return count;
			}
		}

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
		public static bool UpOrDownHeld => Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);

		public static bool Find => Input.GetKeyDown(KeyCode.F) &&
			(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
			 Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand));

		/// <summary>
		/// Where a click or touch began this frame, in screen pixels. Touches arrive as the mouse.
		/// </summary>
		public static bool TryGetPress(out Vector2 position)
		{
			position = Input.mousePosition;
			return Input.GetMouseButtonDown(0);
		}

		/// <summary>
		/// How many fingers are touching the screen.
		/// </summary>
		public static int TouchCount => Input.touchCount;
#else
		public static bool Escape => false;
		public static bool Up => false;
		public static bool Down => false;
		public static bool Enter => false;
		public static bool Shift => false;
		public static bool UpOrDownHeld => false;
		public static bool Find => false;
		public static int TouchCount => 0;

		public static bool TryGetPress(out Vector2 position)
		{
			position = default;
			return false;
		}
#endif
	}
}
