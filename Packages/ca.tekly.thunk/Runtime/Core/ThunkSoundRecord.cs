using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// The state every copy of one sound shares while the game runs: when it last played, how many voices of it are
	/// alive, and where each of its clip sets is in its selection order. It holds no reference to the sound, so
	/// copies of the sound loaded from different bundles can come and go freely.
	/// </summary>
	internal sealed class ThunkSoundRecord
	{
		// Exhaustive selection tracks used clips in a 64 bit mask
		private const int MAX_EXHAUSTIVE_CLIPS = 64;

		public double LastPlayTime = double.NegativeInfinity;
		public int LiveCount;

		private RandomSelector64[] m_selectors;

		public bool IsOnCooldown(double now, float cooldown)
		{
			return cooldown > 0f && now - LastPlayTime < cooldown;
		}

		public bool IsFull(int maxInstances)
		{
			return maxInstances > 0 && LiveCount >= maxInstances;
		}

		/// <summary>
		/// Picks the next clip from a clip set. The setIndex identifies the set within its sound, so each set keeps
		/// its own place in its selection order.
		/// </summary>
		public AudioClip Select(int setIndex, in ThunkClipSet clipSet)
		{
			var count = clipSet.Count;

			if (count == 0) {
				return null;
			}

			if (count == 1) {
				return clipSet.Clips[0];
			}

			var mode = clipSet.Mode;

			if (mode == RandomSelectMode.Exhaustive && count > MAX_EXHAUSTIVE_CLIPS) {
				mode = RandomSelectMode.NonRepeating;
			}

			EnsureSelectorCapacity(setIndex + 1);

			// A selector that was never built has a Size of 0. The set can also change underneath the record, either
			// because it was edited or because a copy of the sound from another bundle has different clips.
			if (m_selectors[setIndex].Size != count || m_selectors[setIndex].Mode != mode) {
				m_selectors[setIndex] = new RandomSelector64(count, mode);
			}

			return clipSet.Clips[m_selectors[setIndex].Select()];
		}

		private void EnsureSelectorCapacity(int capacity)
		{
			if (m_selectors != null && m_selectors.Length >= capacity) {
				return;
			}

			var selectors = new RandomSelector64[capacity];
			m_selectors?.CopyTo(selectors, 0);
			m_selectors = selectors;
		}
	}
}
