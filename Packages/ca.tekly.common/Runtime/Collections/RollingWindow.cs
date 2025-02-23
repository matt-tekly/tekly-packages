using System;
using UnityEngine.Assertions;

namespace Tekly.Common.Collections
{
	public class RollingWindow
	{
		public int Count => m_filled ? m_buffer.Length : m_index;
		public int Capacity => m_buffer.Length;

		private readonly float[] m_buffer;
		private readonly float[] m_percentileBuffer;
		private int m_index;
		private bool m_filled;
		private float m_sum;

		public RollingWindow(int capacity, bool supportPercentile = false)
		{
			Assert.IsTrue(capacity > 0, "Capacity must be greater than zero.");
			m_buffer = new float[capacity];

			if (supportPercentile) {
				m_percentileBuffer = new float[capacity];
			}
		}

		public void Add(float value)
		{
			if (m_filled) {
				m_sum -= m_buffer[m_index];
			}

			m_buffer[m_index] = value;
			m_sum += value;

			m_index = (m_index + 1) % m_buffer.Length;

			if (m_index == 0) {
				m_filled = true;
			}
		}

		public float GetRollingAverage()
		{
			return Count == 0 ? 0f : m_sum / Count;
		}

		public float GetPercentile(float percentile)
		{
			Assert.IsNotNull(m_percentileBuffer, "RollingWindow not constructed to support percentiles");

			if (Count == 0) {
				return 0f;
			}

			Array.Copy(m_buffer, m_percentileBuffer, Count);
			Array.Sort(m_percentileBuffer, 0, Count);

			var startIndex = Math.Max(0, (int)Math.Floor(percentile * Count));

			var numValues = Math.Max(1, Count - startIndex);
			var sum = 0f;

			for (var i = startIndex; i < Count; i++) {
				sum += m_percentileBuffer[i];
			}

			return sum / numValues;
		}

		public void Reset()
		{
			m_index = 0;
			m_sum = 0f;
			m_filled = false;
		}
	}
}