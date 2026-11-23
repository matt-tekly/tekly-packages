using NUnit.Framework;
using UnityEngine;

namespace Tekly.Thunk.Tests
{
	public class ThunkTimelineTests : ThunkTestBase
	{
		private const float TOLERANCE = 0.001f;

		[Test]
		public void NullOrEmptyEntries_ReturnNone()
		{
			Assert.IsTrue(Runtime.Play(CreateTimeline(null), null).IsNone);
			Assert.IsTrue(Runtime.Play(CreateTimeline(new TimelineEntryDescription[0]), null).IsNone);
		}

		[Test]
		public void EntriesWithNoClips_ReturnNone()
		{
			var timeline = CreateTimeline(new[] { Entry(clips: 0), Entry(1f, 0) });

			Assert.IsTrue(Runtime.Play(timeline, null).IsNone);
		}

		[Test]
		public void AnEntryWithNoClips_IsSkipped()
		{
			var timeline = CreateTimeline(new[] { Entry(), Entry(clips: 0), Entry(0.5f) });
			var voice = GetVoice(Runtime.Play(timeline, null));

			Assert.AreEqual(2, voice.Sources.Count);
		}

		[Test]
		public void EachEntry_GetsOneNonLoopingSourceWithAClipFromItsOwnSet()
		{
			var timeline = CreateTimeline(new[] { Entry(clips: 2), Entry(1f, 3), Entry(2f) });
			var voice = GetVoice(Runtime.Play(timeline, null));

			Assert.AreEqual(3, voice.Sources.Count);

			for (var index = 0; index < 3; index++) {
				var audio = voice.Sources[index].Audio;

				Assert.IsFalse(audio.loop);
				CollectionAssert.Contains(timeline.Entries[index].Clips.Clips, audio.clip);
			}
		}

		[Test]
		public void Delay_DecidesWhenTheEntryStartsOnTheAudioClock()
		{
			var timeline = CreateTimeline(new[] { Entry(), Entry(100f) });
			var before = AudioSettings.dspTime;
			var voice = GetVoice(Runtime.Play(timeline, null));

			Assert.LessOrEqual(voice.Sources[0].StartDsp, AudioSettings.dspTime);
			Assert.AreEqual(before + 100d, voice.Sources[1].StartDsp, 0.5d);
			Assert.IsTrue(voice.Sources[0].Audio.isPlaying);
			Assert.IsTrue(voice.Sources[1].Audio.isPlaying);
		}

		[Test]
		public void VolumeOffset_ScalesTheEntrysGainAndVolume()
		{
			var timeline = CreateTimeline(new[] { Entry(), Entry(volumeOffset: -0.5f), Entry(volumeOffset: -1f) });
			var voice = GetVoice(Runtime.Play(timeline, null));

			Assert.AreEqual(1f, voice.Sources[0].Gain, TOLERANCE);
			Assert.AreEqual(0.5f, voice.Sources[1].Gain, TOLERANCE);
			Assert.AreEqual(0f, voice.Sources[2].Gain, TOLERANCE);
			Assert.AreEqual(1f, voice.Sources[0].Audio.volume, TOLERANCE);
			Assert.AreEqual(0.5f, voice.Sources[1].Audio.volume, TOLERANCE);
			Assert.AreEqual(0f, voice.Sources[2].Audio.volume, TOLERANCE);
		}

		[Test]
		public void Semitones_ScaleTheEntrysPitch()
		{
			var timeline = CreateTimeline(new[] { Entry(), Entry(semitones: 12f), Entry(semitones: -12f) });
			var voice = GetVoice(Runtime.Play(timeline, null));

			Assert.AreEqual(1f, voice.Sources[0].PitchScale, TOLERANCE);
			Assert.AreEqual(2f, voice.Sources[1].PitchScale, TOLERANCE);
			Assert.AreEqual(0.5f, voice.Sources[2].PitchScale, TOLERANCE);

			for (var index = 0; index < 3; index++) {
				var source = voice.Sources[index];
				Assert.AreEqual(voice.Pitch * source.PitchScale, source.Audio.pitch, TOLERANCE);
			}
		}

		[Test]
		public void Semitones_AreRelativeToTheTimelinesPitch()
		{
			var timeline = CreateTimeline(new[] { Entry(semitones: 12f) }, pitch: 2f);
			var voice = GetVoice(Runtime.Play(timeline, null));

			Assert.AreEqual(4f, voice.Sources[0].Audio.pitch, TOLERANCE);
		}

		[Test]
		public void ChangingTheUserPitch_ScalesEverySourceKeepingItsOwnScale()
		{
			var timeline = CreateTimeline(new[] { Entry(), Entry(semitones: -12f) });
			var voice = GetVoice(Runtime.Play(timeline, null));

			var first = voice.Sources[0].Audio.pitch;
			var second = voice.Sources[1].Audio.pitch;

			voice.UserPitch = 2f;
			voice.Apply();

			Assert.AreEqual(first * 2f, voice.Sources[0].Audio.pitch, TOLERANCE);
			Assert.AreEqual(second * 2f, voice.Sources[1].Audio.pitch, TOLERANCE);
		}

		[Test]
		public void ATimeline_CountsOnceAgainstMaxInstances()
		{
			var timeline = CreateTimeline(new[] { Entry(), Entry(0.5f), Entry(1f) }, maxInstances: 1);

			var first = Runtime.Play(timeline, null);
			var second = Runtime.Play(timeline, null);

			Assert.IsFalse(first.IsNone);
			Assert.IsTrue(second.IsNone);
			Assert.AreEqual(1, Runtime.GetRecord(timeline).LiveCount);
		}

		[Test]
		public void StopVoice_WithNoFade_FreesTheVoice()
		{
			var handle = Runtime.Play(CreateTimeline(new[] { Entry(), Entry(100f) }), null);

			Runtime.StopVoice(GetVoice(handle), 0f);

			Assert.IsFalse(Resolves(handle));
		}

		[Test]
		public void StopVoice_WithAFade_CancelsEntriesThatHaveNotStarted()
		{
			var handle = Runtime.Play(CreateTimeline(new[] { Entry(), Entry(100f) }), null);
			var voice = GetVoice(handle);

			Runtime.StopVoice(voice, 0.5f);

			Assert.AreEqual(ThunkVoiceState.Stopping, voice.State);
			Assert.IsTrue(voice.Sources[0].Audio.isPlaying);
			Assert.IsFalse(voice.Sources[1].Audio.isPlaying);

			Runtime.Tick(1f, 1f);

			Assert.IsFalse(Resolves(handle));
		}

		[Test]
		public void PauseAndResume_MoveDelayedEntriesLaterByThePause()
		{
			var handle = Runtime.Play(CreateTimeline(new[] { Entry(), Entry(100f) }), null);
			var voice = GetVoice(handle);
			var immediate = voice.Sources[0].StartDsp;
			var delayed = voice.Sources[1].StartDsp;

			Runtime.PauseVoice(voice, 0f);

			Assert.AreEqual(ThunkVoiceState.Paused, voice.State);
			Assert.AreEqual(AudioSettings.dspTime, voice.StageDsp, 0.5d);

			Runtime.ResumeVoice(voice, 0f);

			Assert.AreEqual(ThunkVoiceState.Playing, voice.State);
			Assert.GreaterOrEqual(voice.Sources[1].StartDsp, delayed);
			Assert.AreEqual(immediate, voice.Sources[0].StartDsp);
		}

		[Test]
		public void AReusedSource_HasItsTimelineStateReset()
		{
			var emitter = CreateEmitter();
			var timeline = CreateTimeline(new[] { Entry(100f, 1, 0f, 12f) });

			var first = GetVoice(Runtime.Play(timeline, emitter));
			var audio = first.Sources[0].Audio;

			Runtime.StopVoice(first, 0f);

			var second = GetVoice(Runtime.Play(CreateSound(), emitter));
			var source = second.Sources[0];

			Assert.AreSame(audio, source.Audio);
			Assert.AreEqual(1f, source.PitchScale, TOLERANCE);
			Assert.AreEqual(0d, source.StartDsp);
		}

		[Test]
		public void Release_WithNoFadeOut_FreesTheVoice()
		{
			var handle = Runtime.Play(CreateTimeline(new[] { Entry(), Entry(100f) }), null);
			var voice = GetVoice(handle);

			voice.Sound.Release(voice, Runtime);

			Assert.IsFalse(Resolves(handle));
		}

		[Test]
		public void Release_WithAFadeOut_StopsTheVoiceAndCancelsDelayedEntries()
		{
			var handle = Runtime.Play(CreateTimeline(new[] { Entry(), Entry(100f) }, fadeOut: 0.5f), null);
			var voice = GetVoice(handle);

			voice.Sound.Release(voice, Runtime);

			Assert.AreEqual(ThunkVoiceState.Stopping, voice.State);
			Assert.IsFalse(voice.Sources[1].Audio.isPlaying);
		}

		[Test]
		public void ATrack_CanPlayATimelineOver()
		{
			var track = CreateTrack(duckLevel: 0.25f);
			var state = Runtime.GetTrack(track);

			var handle = state.PlayOver(Runtime, track, CreateTimeline(new[] { Entry(), Entry(0.5f) }));

			Assert.IsTrue(Resolves(handle));
			Assert.AreEqual(0.25f, state.DuckGain, TOLERANCE);
			Assert.AreEqual(0, state.Count);
		}
	}
}
