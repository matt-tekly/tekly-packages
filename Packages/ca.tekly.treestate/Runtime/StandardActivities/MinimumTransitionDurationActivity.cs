// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

namespace Tekly.TreeState.StandardActivities
{
	public class MinimumTransitionDurationActivity : TreeStateActivity
	{
		public float MinimumTransitionTime = 2.0f;
		
		protected override bool IsDoneLoading()
		{
			return TreeState.Manager.CurrentTransitionDuration > MinimumTransitionTime;
		}
	}
}