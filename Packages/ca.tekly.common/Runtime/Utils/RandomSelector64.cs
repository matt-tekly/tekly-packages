using System;
using UnityEngine.Assertions;

namespace Tekly.Common.Utils
{
	public enum RandomSelectMode
	{
		Random,
		NonRepeating,
		Exhaustive,
		Sequential
	}

	/// <summary>
	/// Selects indices in the range [0, Size) using a RandomSelectMode.
	///
	/// Exhaustive mode tracks used indices in a 64 bit mask, so it supports at most 64 entries. The other modes
	/// support any size.
	/// </summary>
	public struct RandomSelector64
	{
		public readonly int Size;
		public readonly RandomSelectMode Mode;

		private readonly NumberGenerator m_numberGenerator;
		private readonly ulong m_fullMask;

		private ulong m_state;
		private int m_lastIndex;

		public RandomSelector64(int size, RandomSelectMode mode, NumberGenerator numberGenerator)
		{
			Assert.IsTrue(size > 0, "Size must be greater than 0.");

			// Thrown rather than asserted as a larger size makes Exhaustive mode loop forever.
			if (mode == RandomSelectMode.Exhaustive && size > 64) {
				throw new ArgumentOutOfRangeException(nameof(size), size, "Exhaustive mode supports at most 64 entries.");
			}

			Size = size;
			Mode = mode;
			m_numberGenerator = numberGenerator;
			m_fullMask = size >= 64 ? ulong.MaxValue : (1UL << size) - 1;

			// NonRepeating stores the last index in the state, so start with a value that matches no index.
			m_state = mode == RandomSelectMode.NonRepeating ? ulong.MaxValue : 0UL;
			m_lastIndex = -1;
		}

		public RandomSelector64(int size, RandomSelectMode mode) : this(size, mode, NumberGenerator.FromUnity()) { }

		public int Select()
		{
			if (Size == 1) {
				return 0;
			}

			switch (Mode) {
				case RandomSelectMode.Random: {
					return m_numberGenerator.Range(0, Size);
				}
				case RandomSelectMode.NonRepeating: {
					int newIndex;
					do {
						newIndex = m_numberGenerator.Range(0, Size);
					} while (newIndex == (int)m_state);

					m_state = (ulong)newIndex;
					return newIndex;
				}
				case RandomSelectMode.Exhaustive: {
					if (m_state == m_fullMask) {
						m_state = 0;
					}

					// The first pick of a round can't be the last pick of the previous round, so an entry never plays
					// twice in a row across the round boundary.
					var startingRound = m_state == 0;

					int randomIndex;
					do {
						randomIndex = m_numberGenerator.Range(0, Size);
					} while ((m_state & (1UL << randomIndex)) != 0 || (startingRound && randomIndex == m_lastIndex));

					m_lastIndex = randomIndex;
					m_state |= 1UL << randomIndex;
					return randomIndex;
				}
				case RandomSelectMode.Sequential: {
					var index = (int)m_state;
					if ((uint)index >= (uint)Size) {
						index = 0;
					}

					var next = index + 1;
					if (next >= Size) {
						next = 0;
					}

					m_state = (ulong)next;
					return index;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(Mode), "Invalid selection mode.");
			}
		}
	}
}
