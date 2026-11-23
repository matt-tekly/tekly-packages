using System;
using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// One sound on a <see cref="ThunkTimeline"/>. Every value is neutral at zero, as that is what a new entry
	/// added in the inspector starts with.
	/// </summary>
	[Serializable]
	public struct ThunkTimelineEntry
	{
		[Tooltip("Seconds after the timeline starts that this entry plays.")]
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
		// Where a scheduled source is moved to while the voice is paused
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
					source.StartDsp = now + entry.Delay;
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

		internal override void PauseSources(ThunkVoice voice)
		{
			var now = AudioSettings.dspTime;

			// Remembered so the resume knows how long the pause lasted
			voice.StageDsp = now;

			for (var index = 0; index < voice.Sources.Count; index++) {
				var source = voice.Sources[index];

				if (source.StartDsp > now) {
					// Not started, so there is nothing to pause: it is pushed out of the way until the resume
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
