using System;

namespace Tekly.Thunk
{
	/// <summary>
	/// Refers to one playing voice: a slot plus the generation that slot had when the voice started. Once the voice
	/// ends the slot's generation moves on, so every copy of the handle goes stale and its calls do nothing.
	/// <c>default</c> is <see cref="None"/>. Ignoring the handle is how a sound is played as a one-shot.
	/// </summary>
	public readonly struct ThunkHandle : IEquatable<ThunkHandle>
	{
		public static readonly ThunkHandle None = default;

		public bool IsNone => Generation == 0;

		/// <summary>
		/// True until the voice ends. A paused voice still counts as playing.
		/// </summary>
		public bool IsPlaying => ThunkRuntime.TryResolve(this, out _);

		public bool IsPaused => ThunkRuntime.TryResolve(this, out var voice) && voice.IsPaused;

		internal readonly int Index;
		internal readonly uint Generation;

		internal ThunkHandle(int index, uint generation)
		{
			Index = index;
			Generation = generation;
		}

		/// <summary>
		/// Stops the whole voice now.
		/// </summary>
		public void Stop()
		{
			Stop(0f);
		}

		/// <summary>
		/// Fades the whole voice out over the given seconds, then stops it.
		/// </summary>
		public void Stop(float fade)
		{
			if (ThunkRuntime.TryResolve(this, out var voice)) {
				ThunkRuntime.Instance.StopVoice(voice, fade);
			}
		}

		/// <summary>
		/// Lets the voice end the way its sound is meant to end. A clip fades out using the sound's fade out time and
		/// a chain plays its end.
		/// </summary>
		public void Release()
		{
			if (ThunkRuntime.TryResolve(this, out var voice)) {
				voice.Sound.Release(voice, ThunkRuntime.Instance);
			}
		}

		public void Pause()
		{
			Pause(0f);
		}

		/// <summary>
		/// Fades the voice out over the given seconds, then pauses it where it is.
		/// </summary>
		public void Pause(float fade)
		{
			if (ThunkRuntime.TryResolve(this, out var voice)) {
				ThunkRuntime.Instance.PauseVoice(voice, fade);
			}
		}

		public void Resume()
		{
			Resume(0f);
		}

		/// <summary>
		/// Resumes a paused voice and fades it back in over the given seconds.
		/// </summary>
		public void Resume(float fade)
		{
			if (ThunkRuntime.TryResolve(this, out var voice)) {
				ThunkRuntime.Instance.ResumeVoice(voice, fade);
			}
		}

		/// <summary>
		/// Multiplies the volume the sound chose for this voice.
		/// </summary>
		public void SetVolume(float volume)
		{
			if (ThunkRuntime.TryResolve(this, out var voice)) {
				voice.UserVolume = volume;
				voice.Apply();
			}
		}

		/// <summary>
		/// Multiplies the pitch the sound chose for this voice.
		/// </summary>
		public void SetPitch(float pitch)
		{
			if (ThunkRuntime.TryResolve(this, out var voice)) {
				voice.UserPitch = pitch;
				voice.Apply();
			}
		}

		public bool Equals(ThunkHandle other)
		{
			return Index == other.Index && Generation == other.Generation;
		}

		public override bool Equals(object obj)
		{
			return obj is ThunkHandle other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (Index * 397) ^ (int) Generation;
		}

		public static bool operator ==(ThunkHandle a, ThunkHandle b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(ThunkHandle a, ThunkHandle b)
		{
			return !a.Equals(b);
		}

		public override string ToString()
		{
			return IsNone ? "ThunkHandle.None" : $"ThunkHandle({Index}:{Generation})";
		}
	}
}
