// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using UnityEngine;

namespace Tekly.TreeState.StandardActivities
{
	public class MinimumLoadTimeActivity : TreeStateActivity
	{
		public float MinimumLoadTime = 2.0f;
		public float MinimumUnloadTime = 2.0f;
        
		private float m_loadTimer;
		private float m_unloadTimer;

		protected override void LoadingStarted()
		{
			m_loadTimer = MinimumLoadTime;
		}

		protected override void LoadingUpdate()
		{
			m_loadTimer -= Time.unscaledDeltaTime;
		}

		protected override bool IsDoneLoading()
		{
			return m_loadTimer <= 0;
		}
		
		
		protected override void UnloadingStarted()
		{
			m_unloadTimer = MinimumUnloadTime;
		}

		protected override void UnloadingUpdate()
		{
			m_unloadTimer -= Time.unscaledDeltaTime;
		}

		protected override bool IsDoneUnloading()
		{
			return m_unloadTimer <= 0;
		}
	}
}