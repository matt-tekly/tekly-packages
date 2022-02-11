// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System.Collections.Generic;
using Tekly.Logging;

namespace Tekly.TreeState.Sequences
{
	public class Sequencer : ISequence
	{
		public ISequence ActiveSequence { get; private set; }
		
		private readonly List<ISequence> m_sequences;

		private bool m_hasBegan;
		private bool m_isDone;

		private int m_index = -1;
		
		public Sequencer(List<ISequence> sequences)
		{
			m_sequences = sequences;
		}

		public void Begin()
		{
			if (m_hasBegan) {
				TkLogger.Get<Sequencer>().Error("Trying to Begin a Sequencer twice.");
				return;
			}

			m_hasBegan = true;
			
			BeginNextSequence();
		}

		public bool IsDone()
		{
			return m_isDone;
		}

		public bool Update()
		{
			if (m_hasBegan == false) {
				return false;
			}
 
			if (m_isDone) {
				return true;
			}

			if (ActiveSequence.Update()) {
				BeginNextSequence();
			}

			return m_isDone;
		}

		private void BeginNextSequence()
		{
			m_index++;

			if (m_index < m_sequences.Count) {
				ActiveSequence = m_sequences[m_index];
				ActiveSequence.Begin();
			} else {
				ActiveSequence = null;
				m_isDone = true;
			}
		}
	}
}