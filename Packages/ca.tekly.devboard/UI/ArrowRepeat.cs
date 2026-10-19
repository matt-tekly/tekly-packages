using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Up and Down with key repeat, the way a text field's arrows behave: once on the press, then again and again
	/// while the key stays held, after a short delay. One per thing that listens, since each keeps its own timing.
	/// </summary>
	internal sealed class ArrowRepeat
	{
		// Matches typical OS key repeat: a pause before repeating, then several steps a second
		private const float DELAY = 0.4f;
		private const float INTERVAL = 0.05f;

		private float m_nextUp;
		private float m_nextDown;

		/// <summary>
		/// True on the frame Up is pressed, and on each repeat while it's held.
		/// </summary>
		public bool Up()
		{
			return Step(KeyInput.Up, KeyInput.UpHeld, ref m_nextUp);
		}

		/// <summary>
		/// True on the frame Down is pressed, and on each repeat while it's held.
		/// </summary>
		public bool Down()
		{
			return Step(KeyInput.Down, KeyInput.DownHeld, ref m_nextDown);
		}

		private static bool Step(bool pressed, bool held, ref float nextRepeat)
		{
			var now = Time.unscaledTime;

			if (pressed) {
				nextRepeat = now + DELAY;
				return true;
			}

			if (!held) {
				return false;
			}

			if (now < nextRepeat) {
				return false;
			}

			nextRepeat = now + INTERVAL;
			return true;
		}
	}
}
