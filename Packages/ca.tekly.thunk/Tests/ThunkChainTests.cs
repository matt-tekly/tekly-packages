using NUnit.Framework;
using UnityEngine;

namespace Tekly.Thunk.Tests
{
	public class ThunkChainTests : ThunkTestBase
	{
		private const double TOLERANCE = 0.05d;

		[Test]
		public void NoLoopClips_ReturnsNone()
		{
			var chain = CreateChain(loopClips: 0);

			Assert.IsTrue(Runtime.Play(chain, null).IsNone);
		}

		[Test]
		public void LoopOnly_PlaysOneLoopingSourceInTheLoopStage()
		{
			var chain = CreateChain();
			var voice = GetVoice(Runtime.Play(chain, null));

			Assert.AreEqual(1, voice.Sources.Count);
			Assert.IsTrue(voice.Sources[0].Audio.loop);
			Assert.AreEqual(ThunkChain.STAGE_LOOP, voice.Stage);
			Assert.IsFalse(voice.WatchPitch);
		}

		[Test]
		public void StartAndLoop_SchedulesTheLoopAfterTheStart()
		{
			var chain = CreateChain(startClips: 1);
			var before = AudioSettings.dspTime;
			var voice = GetVoice(Runtime.Play(chain, null));

			Assert.AreEqual(2, voice.Sources.Count);
			Assert.AreSame(chain.Start.Clips[0], voice.Sources[0].Audio.clip);
			Assert.IsFalse(voice.Sources[0].Audio.loop);
			Assert.AreSame(chain.Loop.Clips[0], voice.Sources[1].Audio.clip);
			Assert.IsTrue(voice.Sources[1].Audio.loop);
			Assert.AreEqual(ThunkChain.STAGE_START, voice.Stage);
			Assert.IsTrue(voice.WatchPitch);
			Assert.Greater(voice.StageDsp, before);
			Assert.AreEqual(before + 0.03d + 1d, voice.StageDsp, TOLERANCE);
		}

		[Test]
		public void Pitch_ChangesHowFarAheadTheLoopIsScheduled()
		{
			var chain = CreateChain(startClips: 1, pitch: 2f);
			var before = AudioSettings.dspTime;
			var voice = GetVoice(Runtime.Play(chain, null));

			Assert.AreEqual(before + 0.03d + 0.5d, voice.StageDsp, TOLERANCE);
		}

		[Test]
		public void UpdateStage_MovesToTheLoopOnceTheStartHasPassed()
		{
			var voice = GetVoice(Runtime.Play(CreateChain(startClips: 1), null));

			voice.StageDsp = 0d;
			ThunkChain.UpdateStage(voice);

			Assert.AreEqual(ThunkChain.STAGE_LOOP, voice.Stage);
			Assert.IsFalse(voice.WatchPitch);
		}

		[Test]
		public void UpdateStage_LeavesTheStartStageWhileTheStartIsPlaying()
		{
			var voice = GetVoice(Runtime.Play(CreateChain(startClips: 1), null));

			ThunkChain.UpdateStage(voice);

			Assert.AreEqual(ThunkChain.STAGE_START, voice.Stage);
			Assert.IsTrue(voice.WatchPitch);
		}

		[Test]
		public void Release_InTheLoopStage_PlaysTheEndAndFadesTheLoop()
		{
			var chain = CreateChain(startClips: 1, endClips: 1, endCrossfade: 0.5f);
			var handle = Runtime.Play(chain, null);
			var voice = GetVoice(handle);

			voice.StageDsp = 0d;
			voice.Sound.Release(voice, Runtime);

			Assert.IsTrue(voice.Released);
			Assert.AreEqual(ThunkChain.STAGE_END, voice.Stage);
			Assert.AreEqual(3, voice.Sources.Count);
			Assert.AreSame(chain.End.Clips[0], voice.Sources[2].Audio.clip);
			Assert.IsFalse(voice.Sources[2].Audio.loop);
			Assert.Greater(voice.Sources[1].FadeDuration, 0f);
			Assert.IsTrue(voice.HasSourceFades);
			Assert.IsTrue(Resolves(handle));
		}

		[Test]
		public void Tick_FinishesTheSourceFade()
		{
			var chain = CreateChain(startClips: 1, endClips: 1, endCrossfade: 0.5f);
			var voice = GetVoice(Runtime.Play(chain, null));

			voice.StageDsp = 0d;
			voice.Sound.Release(voice, Runtime);

			// Held first as the tick may free the voice, which would clear its sources
			var loop = voice.Sources[1];

			Runtime.Tick(1f, 1f);

			Assert.IsFalse(voice.HasSourceFades);
			Assert.AreEqual(0f, loop.Audio.volume, 0.0001f);
		}

		[Test]
		public void Release_WithNoCrossfade_DoesNotStartASourceFade()
		{
			var chain = CreateChain(endClips: 1, endCrossfade: 0f);
			var voice = GetVoice(Runtime.Play(chain, null));

			voice.Sound.Release(voice, Runtime);

			Assert.IsFalse(voice.HasSourceFades);
			Assert.AreEqual(ThunkChain.STAGE_END, voice.Stage);
		}

		[Test]
		public void Release_InTheStartStage_StopsTheScheduledLoop()
		{
			var chain = CreateChain(startClips: 1, endClips: 1);
			var voice = GetVoice(Runtime.Play(chain, null));

			voice.Sound.Release(voice, Runtime);

			Assert.AreEqual(ThunkChain.STAGE_END, voice.Stage);
			Assert.AreEqual(3, voice.Sources.Count);
			Assert.IsFalse(voice.Sources[1].Audio.isPlaying);
		}

		[Test]
		public void Release_Twice_AddsOnlyOneEndSource()
		{
			var chain = CreateChain(endClips: 1);
			var voice = GetVoice(Runtime.Play(chain, null));

			voice.Sound.Release(voice, Runtime);
			voice.Sound.Release(voice, Runtime);

			Assert.AreEqual(2, voice.Sources.Count);
		}

		[Test]
		public void Release_WithNoEndAndNoFadeOut_FreesTheVoice()
		{
			var handle = Runtime.Play(CreateChain(), null);
			var voice = GetVoice(handle);

			voice.Sound.Release(voice, Runtime);

			Assert.IsFalse(Resolves(handle));
		}

		[Test]
		public void Release_WithNoEndAndAFadeOut_StopsTheVoiceWithAFade()
		{
			var handle = Runtime.Play(CreateChain(fadeOut: 0.5f), null);
			var voice = GetVoice(handle);

			voice.Sound.Release(voice, Runtime);

			Assert.AreEqual(ThunkVoiceState.Stopping, voice.State);
			Assert.IsTrue(Resolves(handle));
		}

		[Test]
		public void Release_OnAPausedVoice_StopsItWithoutPlayingTheEnd()
		{
			var handle = Runtime.Play(CreateChain(endClips: 1), null);
			var voice = GetVoice(handle);

			Runtime.PauseVoice(voice, 0f);
			voice.Sound.Release(voice, Runtime);

			Assert.IsFalse(Resolves(handle));
		}

		[Test]
		public void StopVoice_FreesAChainInEveryStage()
		{
			var start = Runtime.Play(CreateChain(startClips: 1), null);
			var loop = Runtime.Play(CreateChain(), null);
			var end = Runtime.Play(CreateChain(endClips: 1), null);

			var endVoice = GetVoice(end);
			endVoice.Sound.Release(endVoice, Runtime);

			Assert.AreEqual(ThunkChain.STAGE_START, GetVoice(start).Stage);
			Assert.AreEqual(ThunkChain.STAGE_LOOP, GetVoice(loop).Stage);
			Assert.AreEqual(ThunkChain.STAGE_END, endVoice.Stage);

			Runtime.StopVoice(GetVoice(start), 0f);
			Runtime.StopVoice(GetVoice(loop), 0f);
			Runtime.StopVoice(endVoice, 0f);

			Assert.IsFalse(Resolves(start));
			Assert.IsFalse(Resolves(loop));
			Assert.IsFalse(Resolves(end));
		}

		[Test]
		public void PauseAndResume_InTheStartStage_HoldsAndReschedulesTheLoop()
		{
			var voice = GetVoice(Runtime.Play(CreateChain(startClips: 1), null));

			Runtime.PauseVoice(voice, 0f);

			Assert.AreEqual(ThunkVoiceState.Paused, voice.State);
			Assert.AreEqual(double.MaxValue, voice.StageDsp);

			Runtime.ResumeVoice(voice, 0f);

			Assert.AreEqual(ThunkVoiceState.Playing, voice.State);
			Assert.Less(voice.StageDsp, double.MaxValue);
			Assert.Greater(voice.StageDsp, AudioSettings.dspTime);
			Assert.AreEqual(ThunkChain.STAGE_START, voice.Stage);
		}

		[Test]
		public void PauseAndResume_InTheLoopStage_StaysInTheLoopStage()
		{
			var voice = GetVoice(Runtime.Play(CreateChain(startClips: 1), null));

			voice.StageDsp = 0d;
			ThunkChain.UpdateStage(voice);

			Runtime.PauseVoice(voice, 0f);
			Runtime.ResumeVoice(voice, 0f);

			Assert.AreEqual(ThunkChain.STAGE_LOOP, voice.Stage);
		}

		[Test]
		public void PitchChange_InTheStartStage_ReschedulesTheLoop()
		{
			var voice = GetVoice(Runtime.Play(CreateChain(startClips: 1), null));
			var before = voice.StageDsp;

			voice.UserPitch = 2f;
			voice.Apply();

			Assert.Less(voice.StageDsp, before);
		}

		[Test]
		public void PitchChange_InTheLoopStage_LeavesTheStageTimeAlone()
		{
			var voice = GetVoice(Runtime.Play(CreateChain(startClips: 1), null));

			voice.StageDsp = 0d;
			ThunkChain.UpdateStage(voice);

			voice.UserPitch = 2f;
			voice.Apply();

			Assert.AreEqual(0d, voice.StageDsp);
		}

		[Test]
		public void AChain_CountsOnceAgainstMaxInstances()
		{
			var chain = CreateChain(startClips: 1, maxInstances: 1);
			var first = Runtime.Play(chain, null);
			var second = Runtime.Play(chain, null);

			Assert.AreEqual(2, GetVoice(first).Sources.Count);
			Assert.IsTrue(second.IsNone);
			Assert.AreEqual(1, Runtime.GetRecord(chain).LiveCount);
		}

		[Test]
		public void ATrack_CanPlayAChain()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);

			var handle = state.Play(Runtime, track, CreateChain(startClips: 1), 0f);

			Assert.IsTrue(Resolves(handle));
			Assert.AreEqual(1, state.Count);
		}
	}
}
