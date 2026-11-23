using Tekly.Common.Maths;
using Tekly.Common.Utils;
using Tekly.Common.Utils.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace Tekly.Thunk
{
	/// <summary>
	/// What happens when a sound is asked to play while it already has its maximum number of voices.
	/// </summary>
	public enum ThunkStealing
	{
		/// <summary>The new play is ignored.</summary>
		None,

		/// <summary>The voice that started first is stopped to make room.</summary>
		Oldest,

		/// <summary>The voice with the lowest volume is stopped to make room. Distance is not considered.</summary>
		Quietest
	}

	/// <summary>
	/// Something that can be played. Holds the settings every kind of sound shares. The limits (max instances and
	/// cooldown) apply to the sound as a whole, wherever it is played from.
	/// </summary>
	public abstract class ThunkSound : UniqueIdScriptableObject
	{
		public AudioMixerGroup MixerGroup => m_mixerGroup;
		public FloatRange Volume => m_volume;
		public FloatRange Pitch => m_pitch;
		public float FadeIn => m_fadeIn;
		public float FadeOut => m_fadeOut;
		public Ease FadeEase => m_fadeEase;
		public int Priority => m_priority;
		public ThunkClock Clock => m_clock;
		public AudioSource SpatialTemplate => m_spatialTemplate;
		public int MaxInstances => m_maxInstances;
		public ThunkStealing Stealing => m_stealing;
		public float Cooldown => m_cooldown;

		[FormerlySerializedAs("MixerGroup")]
		[SerializeField] private AudioMixerGroup m_mixerGroup;

		[FormerlySerializedAs("Volume")]
		[RangeLimits(0f, 1f)]
		[SerializeField] private FloatRange m_volume = new FloatRange(1f, 1f);

		[FormerlySerializedAs("Pitch")]
		[RangeLimits(0f, 3f)]
		[SerializeField] private FloatRange m_pitch = new FloatRange(1f, 1f);

		[Tooltip("Seconds to fade in when the sound starts.")]
		[Min(0f)]
		[SerializeField] private float m_fadeIn;

		[Tooltip("Seconds to fade out when the sound is released.")]
		[Min(0f)]
		[SerializeField] private float m_fadeOut;

		[SerializeField] private Ease m_fadeEase = Ease.Linear;

		[Tooltip("AudioSource priority. 0 is the most important, 256 the least.")]
		[Range(0, 256)]
		[SerializeField] private int m_priority = 128;

		[FormerlySerializedAs("m_playWhilePaused")]
		[Tooltip("Game Time follows the game: the sound plays at the time scale, and pauses while the time scale is 0 or AudioListener.pause is set. Real Time ignores both. Use Real Time for UI and music.")]
		[SerializeField] private ThunkClock m_clock;

		[Tooltip("Optional. An AudioSource in a prefab. When this sound is played from an emitter or at a position it is played through copies of that prefab, so its 3D sound settings and any filters on it apply. Without one the emitter's template is used. Has no effect on a sound played with no position.")]
		[SerializeField] private AudioSource m_spatialTemplate;

		[FormerlySerializedAs("InstanceCapacity")]
		[Tooltip("How many voices of this sound can play at once, across everything that plays it. 0 is unlimited.")]
		[Min(0)]
		[SerializeField] private int m_maxInstances;

		[Tooltip("What to do when the sound is played while it already has its maximum number of voices.")]
		[SerializeField] private ThunkStealing m_stealing;

		[FormerlySerializedAs("MinimumTimeBetweenPlays")]
		[Tooltip("Seconds that must pass before the sound can play again. A small value stops copies of the sound from stacking up at the same instant.")]
		[Min(0f)]
		[SerializeField] private float m_cooldown = 0.03f;

		/// <summary>
		/// Plays the sound with no position. Ignore the handle to play it as a one-shot.
		/// </summary>
		public ThunkHandle Play()
		{
			var runtime = ThunkRuntime.Instance;
			return runtime != null ? runtime.Play(this, null) : ThunkHandle.None;
		}

		/// <summary>
		/// Plays the sound at a point in the world, through a copy of the sound's spatial template. A sound with
		/// no template is not positioned, so it plays the same as <see cref="Play"/>.
		/// </summary>
		public ThunkHandle PlayAt(Vector3 position)
		{
			var runtime = ThunkRuntime.Instance;
			return runtime != null ? runtime.PlayAt(this, position) : ThunkHandle.None;
		}

		/// <summary>
		/// False for a sound that keeps playing until it is told to end. Those are released when the emitter
		/// playing them goes away.
		/// </summary>
		internal virtual bool EndsOnItsOwn => true;

		/// <summary>
		/// Starts the voice's sources. Returns false if there is nothing to play.
		/// </summary>
		internal abstract bool Begin(ThunkVoice voice, ThunkSoundRecord record);

		/// <summary>
		/// Called when a voice starts fading out to a stop.
		/// </summary>
		internal virtual void Stopping(ThunkVoice voice) { }

		internal virtual void PauseSources(ThunkVoice voice)
		{
			voice.PauseSources();
		}

		internal virtual void ResumeSources(ThunkVoice voice)
		{
			voice.UnPauseSources();
		}

		/// <summary>
		/// Called when the speed of the voice's clock changes, after the voice's pitch has been updated. For sounds
		/// that have set times ahead of them to move.
		/// </summary>
		internal virtual void ClockRateChanged(ThunkVoice voice, float from, float to) { }

		/// <summary>
		/// Called when the pitch changes on a voice that asked for pitch changes to be reported.
		/// </summary>
		internal virtual void PitchChanged(ThunkVoice voice) { }

		/// <summary>
		/// Ends the voice the way this kind of sound ends naturally.
		/// </summary>
		internal virtual void Release(ThunkVoice voice, ThunkRuntime runtime)
		{
			runtime.StopVoice(voice, m_fadeOut);
		}
	}
}
