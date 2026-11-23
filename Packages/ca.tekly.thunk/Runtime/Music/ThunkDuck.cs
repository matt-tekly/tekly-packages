namespace Tekly.Thunk
{
	/// <summary>
	/// A request to keep a track's music turned down, returned by <see cref="ThunkTrack.Duck()"/>. The music stays
	/// down until the request is released. <c>default</c> is a request that was never made. Releasing it does
	/// nothing, and neither does releasing a request twice.
	/// </summary>
	public readonly struct ThunkDuck
	{
		public bool IsActive => m_state != null && m_state.IsDucking(m_id);

		private readonly ThunkTrackState m_state;
		private readonly int m_id;

		internal ThunkDuck(ThunkTrackState state, int id)
		{
			m_state = state;
			m_id = id;
		}

		/// <summary>
		/// Lets the music come back up, unless another request is still holding it down.
		/// </summary>
		public void Release()
		{
			var runtime = ThunkRuntime.Instance;

			if (m_state != null && runtime != null) {
				m_state.ReleaseDuck(runtime, m_id);
			}
		}
	}
}
