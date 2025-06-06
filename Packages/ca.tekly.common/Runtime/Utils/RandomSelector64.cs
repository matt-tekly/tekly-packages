using System;
using UnityEngine.Assertions;

namespace Tekly.Common.Utils
{
	public enum RandomSelectMode
	{
		Random,
		NonRepeating,
		Exhaustive,
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

		public RandomSelector64(int size, RandomSelectMode mode, NumberGenerator numberGenerator)
		{
			Assert.IsTrue(size > 0, "Size must be greater than 0.");
			
			if (mode == RandomSelectMode.Exhaustive) {
				Assert.IsTrue(size <= 64, "Size must be between 1 and 64.");	
			}
			
			Size = size;
			Mode = mode;
			m_numberGenerator = numberGenerator;
			
			m_state = mode == RandomSelectMode.Exhaustive ? 0UL : ulong.MaxValue;
		}

		public RandomSelector64(int size, RandomSelectMode mode) : this(size, mode, NumberGenerator.FromUnity()) { }

		public int Select()
		{
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
						}
					} else {
						if (m_state == (1UL << Size) - 1) {
							m_state = 0;
						}
					}
					
					int randomIndex;
					do {
						randomIndex = m_numberGenerator.Range(0, Size);
					} while ((m_state & (1UL << randomIndex)) != 0);

					m_state |= 1UL << randomIndex;
					return randomIndex;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(Mode), "Invalid selection mode.");
			}
		}
	}
}