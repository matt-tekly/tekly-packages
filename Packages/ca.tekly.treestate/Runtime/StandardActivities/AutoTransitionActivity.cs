// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using UnityEngine;

namespace Tekly.TreeState.StandardActivities
{
	public class AutoTransitionActivity : TreeStateActivity
	{
		public float DelayTime = 1.0f;
		public string Transition;

		private float m_timer;
		private bool m_transitioned;

		protected override void ActiveStarted()
		{
			m_timer = DelayTime;
			m_transitioned = false;
		}

		protected override void ActiveUpdate()
		{
			m_timer -= Time.unscaledDeltaTime;

			if (m_timer <= 0 && !m_transitioned) {
				TreeState.Manager.HandleTransition(Transition);
				m_transitioned = true;
			}
		}
	}
}