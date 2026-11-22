using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.Audio;

namespace Tekly.Thunk
{
	/// <summary>
	/// One stream of music. A track has a main sound, and can be interrupted by another sound that holds the main
	/// one until it ends. Only the newest sound on a track is audible.
	/// </summary>
	[CreateAssetMenu(fileName = "thunk_track", menuName = "Tekly/Thunk/Track")]
	public class ThunkTrack : UniqueIdScriptableObject
	{
		public float Crossfade => m_crossfade;
		public bool ResumeHeld => m_resumeHeld;
		public AudioMixerGroup MixerGroup => m_mixerGroup;
		public float DuckLevel => m_duckLevel;
		public float DuckFadeDown => m_duckFadeDown;
		public float DuckFadeBack => m_duckFadeBack;

		[Tooltip("Seconds to crossfade when no fade is given, and to fade held music back in.")]
		[Min(0f)]
		[SerializeField] private float m_crossfade = 1f;

		[Tooltip("When an interruption ends, the held music carries on from where it was. Otherwise it starts again.")]
		[SerializeField] private bool m_resumeHeld = true;

		[Tooltip("Optional. Everything played on this track goes to this group in place of the sound's own.")]
		[SerializeField] private AudioMixerGroup m_mixerGroup;

		[Tooltip("How loud the music is while it is ducked, as a ratio of its normal volume.")]
		[Range(0f, 1f)]
		[SerializeField] private float m_duckLevel = 0.3f;

		[Tooltip("Seconds for the music to drop when it is ducked.")]
		[Min(0f)]
		[SerializeField] private float m_duckFadeDown = 0.2f;

		[Tooltip("Seconds for the music to come back up.")]
		[Min(0f)]
		[SerializeField] private float m_duckFadeBack = 0.8f;

		/// <summary>
		/// Makes the sound the track's main music, crossfading from the current main music. Does nothing if the
		/// sound is already the main music. During an interruption the new main music waits its turn.
		/// </summary>
		public ThunkHandle Play(ThunkSound sound)
		{
			return Play(sound, m_crossfade);
		}

		public ThunkHandle Play(ThunkSound sound, float fade)
		{
			var runtime = ThunkRuntime.Instance;
			return runtime != null ? runtime.GetTrack(this).Play(runtime, this, sound, fade) : ThunkHandle.None;
		}

		/// <summary>
		/// Plays the sound over the top of the track, holding whatever was playing. When the returned voice ends or
		/// is stopped, what was held comes back.
		/// </summary>
		public ThunkHandle Interrupt(ThunkSound sound)
		{
			return Interrupt(sound, m_crossfade);
		}

		public ThunkHandle Interrupt(ThunkSound sound, float fade)
		{
			var runtime = ThunkRuntime.Instance;
			return runtime != null ? runtime.GetTrack(this).Interrupt(runtime, this, sound, fade) : ThunkHandle.None;
		}

		/// <summary>
		/// Plays the sound over the music and keeps the music ducked for as long as the sound is heard. The sound
		/// is not part of the track: it uses its own mixer group and is not stopped by <see cref="Stop()"/>.
		/// </summary>
		public ThunkHandle PlayOver(ThunkSound sound)
		{
			var runtime = ThunkRuntime.Instance;
			return runtime != null ? runtime.GetTrack(this).PlayOver(runtime, this, sound) : ThunkHandle.None;
		}

		/// <summary>
		/// Turns the music down to the track's duck level until the returned request is released. With several
		/// requests active the lowest level wins, they do not stack.
		/// </summary>
		public ThunkDuck Duck()
		{
			return Duck(m_duckLevel);
		}

		public ThunkDuck Duck(float level)
		{
			var runtime = ThunkRuntime.Instance;

			if (runtime == null) {
				return default;
			}

			var state = runtime.GetTrack(this);
			return new ThunkDuck(state, state.Duck(runtime, this, level, ThunkHandle.None));
		}

		/// <summary>
		/// Stops everything on the track, held music included.
		/// </summary>
		public void Stop()
		{
			Stop(m_crossfade);
		}

		public void Stop(float fade)
		{
			var runtime = ThunkRuntime.Instance;
			runtime?.GetTrack(this).Stop(runtime, fade);
		}
	}
}
