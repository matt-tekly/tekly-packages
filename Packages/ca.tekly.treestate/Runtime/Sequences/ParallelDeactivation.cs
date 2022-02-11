// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System.Collections.Generic;
using Tekly.Logging;

namespace Tekly.TreeState.Sequences
{
	public class ParallelDeactivation<T> : ISequence where T : TreeActivity
	{
		private readonly List<T> m_activities;

		private bool m_isDone;
		private bool m_hasBegan;
        
		public ParallelDeactivation(List<T> activities)
		{
			m_activities = activities;
		}

		public void Begin()
		{
			if (m_hasBegan) {
				TkLogger.Get<ParallelDeactivation<T>>().Error("Trying to Begin a ParallelDeactivation twice.");
				return;
			}

			m_hasBegan = true;
			
			foreach (var activity in m_activities) {
				activity.Unload();
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
				if (activity.Mode != ActivityMode.Inactive) {
					return false;
				}
			}

			m_isDone = true;

			return m_isDone;
		}
	}
}