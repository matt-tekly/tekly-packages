using NUnit.Framework;
using UnityEngine;

namespace Tekly.Thunk.Tests
{
	public class ThunkClockTests : ThunkTestBase
	{
		private const float TOLERANCE = 0.0001f;
		private const float STEP = 0.02f;

		[Test]
		public void ASound_IsOnTheGameClock_UnlessSomethingAsksForRealTime()
		{
			var voice = GetVoice(Runtime.Play(CreateSound(), CreateEmitter()));

			Assert.AreEqual(ThunkClock.GameTime, voice.Clock);
			Assert.IsFalse(voice.Sources[0].Audio.ignoreListenerPause);
		}

		[Test]
		public void RealTime_OnTheSound_AppliesWhereverItIsPlayed()
		{
			var voice = GetVoice(Runtime.Play(CreateRealTimeSound(), null));

			Assert.AreEqual(ThunkClock.RealTime, voice.Clock);
			Assert.IsTrue(voice.Sources[0].Audio.ignoreListenerPause);
		}

		[Test]
		public void RealTime_OnTheEmitter_AppliesToASoundOnTheGameClock()
		{
			var emitter = CreateEmitter();
			emitter.RealTime = true;

			var voice = GetVoice(Runtime.Play(CreateSound(), emitter));

			Assert.AreEqual(ThunkClock.RealTime, voice.Clock);
			Assert.IsTrue(voice.Sources[0].Audio.ignoreListenerPause);
		}

		[Test]
		public void RealTime_OnTheTrack_AppliesToItsMusicButNotToSoundsPlayedOverIt()
		{
			var track = CreateTrack();
			SetRealTime(track);

			var state = Runtime.GetTrack(track);
			var music = GetVoice(state.Play(Runtime, track, CreateSound(), 0f));
			var over = GetVoice(state.PlayOver(Runtime, track, CreateSound()));

			Assert.AreEqual(ThunkClock.RealTime, music.Clock);
			Assert.AreEqual(ThunkClock.GameTime, over.Clock);
		}

		[Test]
		public void ATrackThatIsNotRealTime_LeavesItsMusicOnTheSoundsClock()
		{
			var track = CreateTrack();
			SetRealTime(track, false);

			var state = Runtime.GetTrack(track);
			var music = GetVoice(state.Play(Runtime, track, CreateSound(), 0f));

			Assert.AreEqual(ThunkClock.GameTime, music.Clock);
		}

		[Test]
		public void GameSpeed_ChangesThePitchOfGameTimeVoicesOnly()
		{
			var game = GetVoice(Runtime.Play(CreateSound(), null));
			var real = GetVoice(Runtime.Play(CreateRealTimeSound(), null));

			Runtime.Tick(STEP * 0.5f, STEP, 0.5f);

			Assert.AreEqual(0.5f, game.Sources[0].Audio.pitch, TOLERANCE);
			Assert.AreEqual(1f, real.Sources[0].Audio.pitch, TOLERANCE);
		}

		[Test]
		public void AVoiceStartedInSlowMotion_PlaysAtTheGameSpeed()
		{
			Runtime.Tick(STEP * 0.5f, STEP, 0.5f);

			var voice = GetVoice(Runtime.Play(CreateSound(), null));

			Assert.AreEqual(0.5f, voice.Sources[0].Audio.pitch, TOLERANCE);
		}

		[Test]
		public void GameClockStopped_PausesGameTimeVoices_AndResumesThem()
		{
			var gameHandle = Runtime.Play(CreateSound(), null);
			var game = GetVoice(gameHandle);
			var real = GetVoice(Runtime.Play(CreateRealTimeSound(), null));

			Runtime.Tick(0f, STEP, 0f);

			Assert.IsTrue(game.SourcesPaused);
			Assert.IsFalse(real.SourcesPaused);
			Assert.IsTrue(Resolves(gameHandle), "a paused voice should not be taken for a finished one");

			Runtime.Tick(STEP, STEP, 1f);

			Assert.IsFalse(game.SourcesPaused);
			Assert.AreEqual(1f, game.Sources[0].Audio.pitch, TOLERANCE);
		}

		[Test]
		public void AVoicePausedByItsHandle_StaysPausedWhenTheGameClockStartsAgain()
		{
			var voice = GetVoice(Runtime.Play(CreateSound(), null));

			Runtime.PauseVoice(voice, 0f);
			Runtime.Tick(0f, STEP, 0f);
			Runtime.Tick(STEP, STEP, 1f);

			Assert.IsTrue(voice.SourcesPaused);

			Runtime.ResumeVoice(voice, 0f);

			Assert.IsFalse(voice.SourcesPaused);
		}

		[Test]
		public void AVoiceResumedByItsHandleWhileTheGameClockIsStopped_WaitsForTheClock()
		{
			var voice = GetVoice(Runtime.Play(CreateSound(), null));

			Runtime.Tick(0f, STEP, 0f);
			Runtime.PauseVoice(voice, 0f);
			Runtime.ResumeVoice(voice, 0f);

			Assert.IsTrue(voice.SourcesPaused);

			Runtime.Tick(STEP, STEP, 1f);

			Assert.IsFalse(voice.SourcesPaused);
		}

		[Test]
		public void AVoiceStartedWhileTheGameClockIsStopped_StartsPaused()
		{
			Runtime.Tick(0f, STEP, 0f);

			var game = GetVoice(Runtime.Play(CreateSound(), null));
			var real = GetVoice(Runtime.Play(CreateRealTimeSound(), null));

			Assert.IsTrue(game.SourcesPaused);
			Assert.IsFalse(real.SourcesPaused);

			Runtime.Tick(STEP, STEP, 1f);

			Assert.IsFalse(game.SourcesPaused);
		}

		[Test]
		public void StoppedWithAFadeWhileTheGameClockIsStopped_EndsAtOnce()
		{
			var handle = Runtime.Play(CreateSound(), null);

			Runtime.Tick(0f, STEP, 0f);
			Runtime.StopVoice(GetVoice(handle), 1f);

			Assert.IsFalse(Resolves(handle));
		}

		[Test]
		public void AFadeOnTheGameClock_DoesNotMoveWhileTheClockIsStopped()
		{
			var voice = GetVoice(Runtime.Play(CreateSound(), null));

			Runtime.StopVoice(voice, 1f);

			// The listener being paused stops the game clock even though game time still passes
			Runtime.Tick(0.5f, 0.5f, 0f);

			Assert.AreEqual(1f, voice.Fade, TOLERANCE);

			Runtime.Tick(0.5f, 0.5f, 1f);

			Assert.AreEqual(0.5f, voice.Fade, TOLERANCE);
		}

		[Test]
		public void AFadeOnTheRealTimeClock_MovesWhileTheGameIsStopped()
		{
			var voice = GetVoice(Runtime.Play(CreateRealTimeSound(), null));

			Runtime.StopVoice(voice, 1f);
			Runtime.Tick(0f, 0.5f, 0f);

			Assert.AreEqual(0.5f, voice.Fade, TOLERANCE);
		}

		[Test]
		public void ATimelineStartedInSlowMotion_WaitsLongerForItsEntries()
		{
			Runtime.Tick(STEP * 0.5f, STEP, 0.5f);

			var before = AudioSettings.dspTime;
			var voice = GetVoice(Runtime.Play(CreateTimeline(new[] { Entry(), Entry(100f) }), null));

			Assert.GreaterOrEqual(voice.Sources[1].StartDsp, before + 199d);
		}

		[Test]
		public void AChangeOfGameSpeed_MovesTimelineEntriesThatHaveNotStarted()
		{
			var voice = GetVoice(Runtime.Play(CreateTimeline(new[] { Entry(), Entry(100f) }), null));
			var started = voice.Sources[0].StartDsp;
			var scheduled = voice.Sources[1].StartDsp;

			Runtime.Tick(STEP * 0.5f, STEP, 0.5f);

			// About 100 seconds were left, which is about 200 at half speed
			Assert.GreaterOrEqual(voice.Sources[1].StartDsp, scheduled + 90d);
			Assert.AreEqual(started, voice.Sources[0].StartDsp);
		}

		[Test]
		public void AChangeOfGameSpeedWhilePaused_MovesTimelineEntriesFromWhereThePauseBegan()
		{
			var voice = GetVoice(Runtime.Play(CreateTimeline(new[] { Entry(), Entry(100f) }), null));

			Runtime.PauseVoice(voice, 0f);

			var pausedAt = voice.StageDsp;
			var left = voice.Sources[1].StartDsp - pausedAt;

			Runtime.Tick(STEP * 0.5f, STEP, 0.5f);

			Assert.AreEqual(left * 2d, voice.Sources[1].StartDsp - pausedAt, 0.001d);
		}

		[Test]
		public void GameRate_IsTheTimeScale_AndZeroWhileStoppedOrListenerPaused()
		{
			Assert.AreEqual(1f, ThunkRuntime.GetGameRate(1f, false, true), TOLERANCE);
			Assert.AreEqual(0.5f, ThunkRuntime.GetGameRate(0.5f, false, true), TOLERANCE);
			Assert.AreEqual(0f, ThunkRuntime.GetGameRate(0f, false, true), TOLERANCE);
			Assert.AreEqual(0f, ThunkRuntime.GetGameRate(1f, true, true), TOLERANCE);
		}

		[Test]
		public void GameRate_WhenNotFollowingTheTimeScale_IsOnlyEverRunningOrStopped()
		{
			Assert.AreEqual(1f, ThunkRuntime.GetGameRate(0.5f, false, false), TOLERANCE);
			Assert.AreEqual(1f, ThunkRuntime.GetGameRate(2f, false, false), TOLERANCE);
			Assert.AreEqual(0f, ThunkRuntime.GetGameRate(0f, false, false), TOLERANCE);
			Assert.AreEqual(0f, ThunkRuntime.GetGameRate(1f, true, false), TOLERANCE);
		}

		private ThunkClip CreateRealTimeSound()
		{
			var sound = CreateSound();
			SetRealTime(sound);

			return sound;
		}
	}
}
