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
	/// Selects numbers randomly up to 64
	/// </summary>
	public struct RandomSelector64
	{
		public readonly int Size;
		public readonly RandomSelectMode Mode;
		private readonly NumberGenerator m_numberGenerator;

		private ulong m_state;
		private short m_lastIndex;

		public RandomSelector64(int size, RandomSelectMode mode, NumberGenerator numberGenerator)
		{
			Assert.IsTrue(size > 0, "Size must be greater than 0.");

			if (mode == RandomSelectMode.Exhaustive) {
				Assert.IsTrue(size <= 64, "Size must be between 1 and 64.");
			}

			Size = size;
			Mode = mode;
			m_numberGenerator = numberGenerator;

			if (mode == RandomSelectMode.Exhaustive || mode == RandomSelectMode.Sequential) {
				m_state = 0UL;
			} else {
				m_state = ulong.MaxValue;
			}
			
			m_lastIndex = 0;
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
					if (Size == 1) {
						return 0;
					}

					int newIndex;
					do {
						newIndex = m_numberGenerator.Range(0, Size);
					} while (newIndex == (int)m_state);

					m_state = (ulong)newIndex;
					return newIndex;
				}
				case RandomSelectMode.Exhaustive: {
					if (Size == 64) {
						if (m_state == ulong.MaxValue) {
							m_state = 0;
							m_state |= 1UL << m_lastIndex;
						}
					} else {
						if (m_state == (1UL << Size) - 1) {
							m_state = 0;
							m_state |= 1UL << m_lastIndex;
						}
					}

					int randomIndex;
					do {
						randomIndex = m_numberGenerator.Range(0, Size);
					} while ((m_state & (1UL << randomIndex)) != 0);

					m_lastIndex = (short)randomIndex;
					m_state |= 1UL << m_lastIndex;
					return randomIndex;
				}
				case RandomSelectMode.Sequential: {
					var index = (int)m_state;
					if ((uint)index >= (uint)Size) {
						index = 0;
					}

					m_lastIndex = (short)index;

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