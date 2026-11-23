using System;
using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// One sound on a <see cref="ThunkTimeline"/>. Every value is neutral at zero, because a new entry added in
	/// the inspector starts with all zeros.
	/// </summary>
	[Serializable]
	public struct ThunkTimelineEntry
	{
		[Tooltip("How many seconds after the timeline starts this entry plays.")]
		[Min(0f)]
		public float Delay;

		public ThunkClipSet Clips;

		[Tooltip("How much quieter this entry is than the timeline's volume. 0 is the same volume, -1 is silent.")]
		[Range(-1f, 0f)]
		public float VolumeOffset;

		[Tooltip("How far this entry is pitched from the timeline's pitch, in semitones.")]
		[Range(-24f, 24f)]
		public float Semitones;
	}

	/// <summary>
	/// A sound made of several clips played at set times after it starts. Entries can overlap. It plays once and
	/// does not loop. The whole timeline is one voice: it counts once against its limits and stopping it stops the
	/// entries that are playing and cancels the ones that haven't started.
	///
	/// Every entry holds an AudioSource for as long as the timeline plays.
	/// </summary>
	[CreateAssetMenu(fileName = "thunk_timeline", menuName = "Tekly/Thunk/Timeline")]
	public class ThunkTimeline : ThunkSound
	{
		// While the voice is paused, a scheduled source is pushed this far into the future
		private const double FAR_AWAY = 1000000d;

		public ThunkTimelineEntry[] Entries => m_entries;

		[SerializeField] private ThunkTimelineEntry[] m_entries;

		internal override bool Begin(ThunkVoice voice, ThunkSoundRecord record)
		{
			if (m_entries == null) {
				return false;
			}

			var now = AudioSettings.dspTime;

			for (var index = 0; index < m_entries.Length; index++) {
				ref var entry = ref m_entries[index];
				var clip = record.Select(index, entry.Clips);

				if (clip == null) {
					continue;
				}

				var source = voice.AddSource(clip, Mathf.Clamp01(1f + entry.VolumeOffset));
				source.PitchScale = Mathf.Pow(2f, entry.Semitones / 12f);
				voice.Apply(source);

				if (entry.Delay > 0f) {
					// Delays are in the voice's own time, so they are longer on the audio clock while it runs slowly
					source.StartDsp = now + entry.Delay / voice.ClockRate;
					source.Audio.PlayScheduled(source.StartDsp);
				} else {
					source.StartDsp = now;
					source.Audio.Play();
				}
			}

			return voice.Sources.Count > 0;
		}

		internal override void Stopping(ThunkVoice voice)
		{
			// Entries that haven't started would otherwise start during the fade out
			var now = AudioSettings.dspTime;

			for (var index = 0; index < voice.Sources.Count; index++) {
				var source = voice.Sources[index];

				if (source.StartDsp > now) {
					source.Audio.Stop();
				}
			}
		}

		internal override void ClockRateChanged(ThunkVoice voice, float from, float to)
		{
			// While paused the entries are parked, and their times are measured from when the pause began
			var paused = voice.SourcesPaused;
			var reference = paused ? voice.StageDsp : AudioSettings.dspTime;
			var scale = from / to;

			for (var index = 0; index < voice.Sources.Count; index++) {
				var source = voice.Sources[index];

				if (source.StartDsp <= reference) {
					continue;
				}

				// What is left of the wait takes longer or shorter at the new speed
				source.StartDsp = reference + (source.StartDsp - reference) * scale;

				if (!paused) {
					source.Audio.SetScheduledStartTime(source.StartDsp);
				}
			}
		}

		internal override void PauseSources(ThunkVoice voice)
		{
			var now = AudioSettings.dspTime;

			// Stored so that resuming can work out how long the pause lasted
			voice.StageDsp = now;

			for (var index = 0; index < voice.Sources.Count; index++) {
				var source = voice.Sources[index];

				if (source.StartDsp > now) {
					// It hasn't started, so there is nothing to pause. Its start is pushed far into the future until the resume.
					source.Audio.SetScheduledStartTime(now + FAR_AWAY);
				} else {
					source.Audio.Pause();
				}
			}
		}

		internal override void ResumeSources(ThunkVoice voice)
		{
			var now = AudioSettings.dspTime;
			var pausedAt = voice.StageDsp;
			var pausedFor = now - pausedAt;

			for (var index = 0; index < voice.Sources.Count; index++) {
				var source = voice.Sources[index];

				if (source.StartDsp > pausedAt) {
					source.StartDsp += pausedFor;
					source.Audio.SetScheduledStartTime(source.StartDsp);
				} else {
					source.Audio.UnPause();
				}
			}
		}
	}
}
