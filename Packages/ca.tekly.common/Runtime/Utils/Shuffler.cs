using System;
using System.Collections.Generic;
using Tekly.Common.Collections;

namespace Tekly.Common.Utils
{
	[Serializable]
	public struct ShufflerState
	{
		public int Remaining;
		public uint Seed;
	}
	
	/// <summary>
	/// Provides getting elements from a collection random exhaustively. Can be persisted correctly as long as the
	/// source collection doesn't change. It will still function with the collection changing but won't be random
	/// exhaustive.
	/// </summary>
	public class Shuffler<T>
	{
		private readonly List<T> m_content = new List<T>();
		private readonly IList<T> m_source;
		
		private readonly NumberGenerator m_numberGenerator;

		public Shuffler(IList<T> source)
		{
			m_source = source;
			m_numberGenerator = new NumberGenerator();
			
			Reset();
		}
		
		public Shuffler(IList<T> source, ShufflerState shufflerState)
		{
			m_source = source;
			m_numberGenerator = new NumberGenerator(shufflerState.Seed);
			Reset(shufflerState);
		}
		
		public T Take()
		{
			if (m_content.Count == 0) {
				m_numberGenerator.ReSeed();
				Reset();
			}

			var item = m_content.Last();
			m_content.RemoveAt(m_content.Count - 1);

			return item;
		}

		public void Reset()
		{
			m_content.Clear();
			m_content.AddRange(m_source);
			m_content.Shuffle(m_numberGenerator);
		}
		
		public void Reset(ShufflerState shufflerState)
		{
			m_content.Clear();
			m_content.AddRange(m_source);
			
			m_numberGenerator.ReSeed(shufflerState.Seed);
			
			m_content.Shuffle(m_numberGenerator);

			var countToRemove = Math.Max(0, m_content.Count - shufflerState.Remaining);
			m_content.RemoveRange(m_content.Count - countToRemove, countToRemove);
		}

		public ShufflerState ToState()
		{
			return new ShufflerState {
				Seed = m_numberGenerator.Seed,
				Remaining = m_content.Count
			};
		}
	}
}