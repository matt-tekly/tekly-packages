// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System.Collections.Generic;
using Tekly.Logging;

namespace Tekly.TreeState.Sequences
{
	public class ParallelActivation<T> : ISequence where T : TreeActivity
	{
		private readonly List<T> m_activities;

		private bool m_isDone;
		private bool m_hasBegan;

		public ParallelActivation(List<T> activities)
		{
			m_activities = activities;
		}

		public void Begin()
		{
			if (m_hasBegan) {
				TkLogger.Get<ParallelActivation<T>>().Error("Trying to Begin a ParallelActivation twice.");
				return;
			}

			m_hasBegan = true;

			foreach (var activity in m_activities) {
				activity.Load();
			}
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

			foreach (var activity in m_activities) {
				if (activity.Mode != ActivityMode.ReadyToActivate) {
					return false;
				}
			}

			m_isDone = true;

			return m_isDone;
		}
	}
}