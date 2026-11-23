namespace Tekly.Thunk
{
	/// <summary>
	/// Choices that apply to every sound. They last for the play session, so set them when the game starts.
	/// </summary>
	public static class ThunkSettings
	{
		/// <summary>
		/// Whether sounds on the Game Time clock speed up and slow down with Time.timeScale. On by default. With it
		/// off they always play at normal speed, and still pause while the time scale is 0 or the listener is
		/// paused. Their fades and timeline delays then count real seconds while the game is running.
		/// </summary>
		public static bool PitchFollowsTimeScale {
			get {
				var runtime = ThunkRuntime.Instance;
				return runtime == null || runtime.PitchFollowsTimeScale;
			}
			set {
				var runtime = ThunkRuntime.Instance;

				if (runtime != null) {
					runtime.PitchFollowsTimeScale = value;
				}
			}
		}
	}
}
