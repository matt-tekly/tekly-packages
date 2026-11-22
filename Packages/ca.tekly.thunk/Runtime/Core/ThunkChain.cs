using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// A sound in three parts: a start that plays once, a loop that follows it with no gap and plays until the
	/// voice is released, and an end that plays on release. The start and end are optional.
	///
	/// The loop is scheduled against the audio clock from the start clip's length, so the join is exact as long as
	/// the pitch doesn't change during the start. If the pitch changes or the voice is paused during the start, the
	/// loop is rescheduled from where the start has got to, which is close but not exact. Compressed formats that
	/// pad their clips (MP3) leave a gap whatever is done here.
	/// </summary>
	[CreateAssetMenu(fileName = "thunk_chain", menuName = "Tekly/Thunk/Chain")]
	public class ThunkChain : ThunkSound
	{
		internal const int STAGE_START = 0;
		internal const int STAGE_LOOP = 1;
		internal const int STAGE_END = 2;

		private const int SET_START = 0;
		private const int SET_LOOP = 1;
		private const int SET_END = 2;

		// Scheduling slightly ahead gives the audio thread time to line both sources up
		private const double SCHEDULE_LEAD = 0.03d;

		// Where a scheduled source is moved to while the voice is paused
		private const double FAR_AWAY = 1000000d;

		public ThunkClipSet Start => m_start;
		public ThunkClipSet Loop => m_loop;
		public ThunkClipSet End => m_end;
		public float EndCrossfade => m_endCrossfade;

		[Tooltip("Optional. Plays once, then the loop follows with no gap.")]
		[SerializeField] private ThunkClipSet m_start;

		[Tooltip("Plays until the voice is released.")]
		[SerializeField] private ThunkClipSet m_loop;

		[Tooltip("Optional. Plays when the voice is released. Without one the voice fades out using the fade out time.")]
		[SerializeField] private ThunkClipSet m_end;

		[Tooltip("Seconds to fade the loop out underneath the end.")]
		[Min(0f)]
		[SerializeField] private float m_endCrossfade = 0.05f;

		internal override bool Begin(ThunkVoice voice, ThunkSoundRecord record)
		{
			var loopClip = record.Select(SET_LOOP, m_loop);

			if (loopClip == null) {
				return false;
			}

			var startClip = record.Select(SET_START, m_start);

			if (startClip == null) {
				var only = voice.AddSource(loopClip);
				only.Audio.loop = true;
				only.Audio.Play();

				voice.Stage = STAGE_LOOP;
				return true;
			}

			// The order matters: while the voice is in the start stage, source 0 is the start and source 1 the loop
			var start = voice.AddSource(startClip);
			var loop = voice.AddSource(loopClip);
			loop.Audio.loop = true;

			var begin = AudioSettings.dspTime + SCHEDULE_LEAD;

			voice.Stage = STAGE_START;
			voice.StageBeginDsp = begin;
			voice.StageDsp = begin + GetDuration(startClip, 0, voice.Pitch);

			start.Audio.PlayScheduled(begin);
			loop.Audio.PlayScheduled(voice.StageDsp);

			voice.StartWatchingPitch();
			return true;
		}

		internal override void Release(ThunkVoice voice, ThunkRuntime runtime)
		{
			if (voice.Released) {
				return;
			}

			UpdateStage(voice);

			// A voice that is paused or already fading away has nothing to play an end over. Checked before an end
			// clip is picked, so a release that plays no end doesn't use up a turn in the end set's order.
			var endClip = voice.State == ThunkVoiceState.Playing ? voice.Record.Select(SET_END, m_end) : null;

			if (endClip == null) {
				runtime.StopVoice(voice, FadeOut);
				return;
			}

			voice.Released = true;
			voice.WatchPitch = false;

			if (voice.Stage == STAGE_START) {
				// The loop never started
				voice.Sources[1].Audio.Stop();
				voice.FadeOutSource(voice.Sources[0], m_endCrossfade);
			} else {
				for (var index = 0; index < voice.Sources.Count; index++) {
					voice.FadeOutSource(voice.Sources[index], m_endCrossfade);
				}
			}

			voice.Stage = STAGE_END;
			voice.AddSource(endClip).Audio.Play();
		}

		internal override void PauseSources(ThunkVoice voice)
		{
			UpdateStage(voice);

			if (voice.Stage != STAGE_START) {
				voice.PauseSources();
				return;
			}

			// The loop hasn't started, so there is nothing to pause: it is pushed out of the way until the resume
			voice.Sources[0].Audio.Pause();
			voice.Sources[1].Audio.SetScheduledStartTime(AudioSettings.dspTime + FAR_AWAY);
			voice.StageDsp = double.MaxValue;
		}

		internal override void ResumeSources(ThunkVoice voice)
		{
			if (voice.Stage != STAGE_START) {
				voice.UnPauseSources();
				return;
			}

			voice.Sources[0].Audio.UnPause();
			RescheduleLoop(voice);
		}

		internal override void PitchChanged(ThunkVoice voice)
		{
			// MaxValue means the voice is paused, and the resume will reschedule
			if (voice.StageDsp == double.MaxValue) {
				return;
			}

			UpdateStage(voice);

			if (voice.Stage == STAGE_START) {
				RescheduleLoop(voice);
			}
		}

		/// <summary>
		/// The voice doesn't get told when the loop takes over from the start, so the stage is worked out from the
		/// audio clock whenever it matters.
		/// </summary>
		internal static void UpdateStage(ThunkVoice voice)
		{
			if (voice.Stage == STAGE_START && AudioSettings.dspTime >= voice.StageDsp) {
				voice.Stage = STAGE_LOOP;
				voice.WatchPitch = false;
			}
		}

		private static void RescheduleLoop(ThunkVoice voice)
		{
			var start = voice.Sources[0].Audio;
			var clip = start.clip;

			// Unity null check: the clip is gone if its bundle was unloaded
			var remaining = clip != null ? GetDuration(clip, start.timeSamples, voice.Pitch) : 0d;

			// During the scheduling lead the start hasn't begun yet, so its time remaining counts from when it will
			voice.StageDsp = System.Math.Max(AudioSettings.dspTime, voice.StageBeginDsp) + remaining;
			voice.Sources[1].Audio.SetScheduledStartTime(voice.StageDsp);
		}

		/// <summary>
		/// Seconds the clip takes to play from a sample position to its end at a pitch.
		/// </summary>
		private static double GetDuration(AudioClip clip, int fromSample, float pitch)
		{
			var rate = clip.frequency * (double) Mathf.Max(Mathf.Abs(pitch), 0.01f);
			return (clip.samples - fromSample) / rate;
		}
	}
}
