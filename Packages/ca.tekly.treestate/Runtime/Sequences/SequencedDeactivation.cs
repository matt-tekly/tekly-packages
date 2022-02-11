// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System.Collections.Generic;
using Tekly.Logging;

namespace Tekly.TreeState.Sequences
{
	public class SequencedDeactivation<T> : ISequence where T : TreeActivity
	{
		private readonly List<T> m_activities;
        private readonly ISequenceMonitor m_sequenceMonitor;
        
        private int m_index;

        private T m_currentActivity;
        private bool m_isDone;
        private bool m_hasBegan;
        
        public SequencedDeactivation(List<T> activities, ISequenceMonitor sequenceMonitor = null)
        {
        	m_activities = activities;
        	m_sequenceMonitor = sequenceMonitor;
        }

        public void Begin()
        {
        	if (m_hasBegan) {
	            TkLogger.Get<SequencedDeactivation<T>>().Error("Trying to Begin a SequencedDeactivation twice.");
	            return;
        	}

			m_hasBegan = true;
        	m_index = -1;
			
        	LoadNextActivity();	
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

        	if (m_currentActivity.Mode == ActivityMode.Inactive) {
        		LoadNextActivity();	
        	}

        	return m_isDone;
        }

        private void LoadNextActivity()
        {
        	m_index++;

        	if (m_index < m_activities.Count) {
        		m_currentActivity = m_activities[m_index];
        		
        		m_sequenceMonitor?.CurrentActivitySet(m_currentActivity);
        		m_currentActivity.Unload();
        	} else {
        		m_currentActivity = null;
        		m_isDone = true;
			}
        }
	}
}