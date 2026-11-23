namespace Tekly.Thunk
{
	/// <summary>
	/// Which clock a sound runs on. Everything about a voice that involves time comes from its clock: how fast it
	/// plays, how its fades advance, when a timeline's entries start and whether it is paused.
	/// </summary>
	public enum ThunkClock
	{
		/// <summary>
		/// Follows the game. The sound plays at Time.timeScale, so it slows down and drops in pitch in slow motion,
		/// and it pauses while the time scale is 0 or AudioListener.pause is set. For anything in the world.
		/// </summary>
		GameTime,

		/// <summary>
		/// Ignores the time scale and AudioListener.pause. For UI, music and anything heard in a pause menu.
		/// </summary>
		RealTime
	}
}
