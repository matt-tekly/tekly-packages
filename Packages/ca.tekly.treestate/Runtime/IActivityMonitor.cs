// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

namespace Tekly.TreeState
{
	public interface IActivityMonitor
	{
		/// <summary>
		/// Loads or unloads that take longer than this many seconds are reported to ActivitySlow. 0 disables it.
		/// </summary>
		float SlowThreshold { get; }

		void ActivityModeChanged(TreeActivity activity, ActivityMode previousMode);

		/// <summary>
		/// Called once when a load or unload passes SlowThreshold while still running (completed is false), and again
		/// when it finishes with the final duration (completed is true).
		/// </summary>
		void ActivitySlow(TreeActivity activity, float duration, bool completed);
	}
}