using System;
using System.Collections.Generic;
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
		private readonly int m_size;
		private readonly RandomSelectMode m_mode;
		private readonly NumberGenerator m_numberGenerator;

		private ulong m_state;

		public RandomSelector64(int size, RandomSelectMode mode, NumberGenerator numberGenerator)
		{
			Assert.IsTrue(size > 0, "Size must be greater than 0.");
			
			if (mode == RandomSelectMode.Exhaustive) {
				Assert.IsTrue(size <= 64, "Size must be between 1 and 64.");	
			}
			
			m_size = size;
			m_mode = mode;
			m_numberGenerator = numberGenerator;
			
			m_state = mode == RandomSelectMode.Exhaustive ? 0UL : ulong.MaxValue;
		}

		public RandomSelector64(int size, RandomSelectMode mode) : this(size, mode, NumberGenerator.FromUnity()) { }

		public int Select()
		{
			switch (m_mode) {
				case RandomSelectMode.Random: {
					return m_numberGenerator.Range(0, m_size);
				}
				case RandomSelectMode.NonRepeating: {
					if (m_size == 1) {
						return 0;
					}
					
					int newIndex;
					do {
						newIndex = m_numberGenerator.Range(0, m_size);
					} while (newIndex == (int)m_state);

					m_state = (ulong)newIndex;
					return newIndex;
				}
				case RandomSelectMode.Exhaustive: {
					if (m_size == 64) {
						if (m_state == ulong.MaxValue) {
							m_state = 0;
						}
					} else {
						if (m_state == (1UL << m_size) - 1) {
							m_state = 0;
						}
					}
					
					int randomIndex;
					do {
						randomIndex = m_numberGenerator.Range(0, m_size);
					} while ((m_state & (1UL << randomIndex)) != 0);

					m_state |= 1UL << randomIndex;
					return randomIndex;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(m_mode), "Invalid selection mode.");
			}
		}
	}
}