using NUnit.Framework;

namespace Tekly.Thunk.Tests
{
	public class ThunkTrackTests : ThunkTestBase
	{
		[Test]
		public void Play_MakesTheSoundTheSingleEntry()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);
			var sound = CreateSound();

			var handle = state.Play(Runtime, track, sound, 0f);

			Assert.IsTrue(Resolves(handle));
			Assert.AreEqual(1, state.Count);
			Assert.AreSame(sound, state.GetSound(0));
			Assert.AreEqual(handle, state.GetHandle(0));
		}

		[Test]
		public void Play_TheSameSoundAgain_ReturnsTheSameHandle()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);
			var sound = CreateSound();

			var first = state.Play(Runtime, track, sound, 0f);
			var second = state.Play(Runtime, track, sound, 0f);

			Assert.AreEqual(first, second);
			Assert.AreEqual(1, state.Count);
			Assert.AreEqual(1, Runtime.GetRecord(sound).LiveCount);
		}

		[Test]
		public void Play_ADifferentSound_ReplacesTheEntryAndStopsTheOldVoice()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);
			var other = CreateSound();

			var first = state.Play(Runtime, track, CreateSound(), 0f);
			var second = state.Play(Runtime, track, other, 0f);

			Assert.IsFalse(Resolves(first));
			Assert.IsTrue(Resolves(second));
			Assert.AreEqual(1, state.Count);
			Assert.AreSame(other, state.GetSound(0));
		}

		[Test]
		public void Interrupt_AddsAnEntryAndPausesTheMainVoice()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);

			var main = state.Play(Runtime, track, CreateSound(), 0f);
			var interrupt = state.Interrupt(Runtime, track, CreateSound(), 0f);

			Assert.AreEqual(2, state.Count);
			Assert.AreEqual(ThunkVoiceState.Paused, GetVoice(main).State);
			Assert.AreEqual(ThunkVoiceState.Playing, GetVoice(interrupt).State);
		}

		[Test]
		public void StoppingTheInterruption_RemovesItAndResumesTheMainVoice()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);

			var main = state.Play(Runtime, track, CreateSound(), 0f);
			var interrupt = state.Interrupt(Runtime, track, CreateSound(), 0f);

			Runtime.StopVoice(GetVoice(interrupt), 0f);

			Assert.AreEqual(1, state.Count);
			Assert.IsFalse(Resolves(interrupt));
			Assert.AreEqual(ThunkVoiceState.Playing, GetVoice(main).State);
		}

		[Test]
		public void Interrupt_WithoutResumeHeld_StopsTheMainVoiceAndStartsANewOneAfterwards()
		{
			var track = CreateTrack(false);
			var state = Runtime.GetTrack(track);
			var mainSound = CreateSound();

			var main = state.Play(Runtime, track, mainSound, 0f);
			var interrupt = state.Interrupt(Runtime, track, CreateSound(), 0f);

			Assert.IsFalse(Resolves(main));
			Assert.AreEqual(2, state.Count);

			Runtime.StopVoice(GetVoice(interrupt), 0f);

			var restarted = state.GetHandle(0);

			Assert.AreEqual(1, state.Count);
			Assert.IsFalse(restarted.IsNone);
			Assert.IsTrue(Resolves(restarted));
			Assert.AreNotEqual(main, restarted);
			Assert.AreSame(mainSound, GetVoice(restarted).Sound);
		}

		[Test]
		public void Play_DuringAnInterruption_SwapsTheMainSoundAndStartsItWhenTheInterruptionStops()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);
			var newMain = CreateSound();

			var main = state.Play(Runtime, track, CreateSound(), 0f);
			var interrupt = state.Interrupt(Runtime, track, CreateSound(), 0f);
			var result = state.Play(Runtime, track, newMain, 0f);

			Assert.IsTrue(result.IsNone);
			Assert.IsFalse(Resolves(main));
			Assert.AreEqual(2, state.Count);
			Assert.AreSame(newMain, state.GetSound(0));
			Assert.IsTrue(state.GetHandle(0).IsNone);
			Assert.AreEqual(0, Runtime.GetRecord(newMain).LiveCount);

			Runtime.StopVoice(GetVoice(interrupt), 0f);

			Assert.AreEqual(1, state.Count);
			Assert.IsTrue(Resolves(state.GetHandle(0)));
			Assert.AreSame(newMain, GetVoice(state.GetHandle(0)).Sound);
		}

		[Test]
		public void StoppingTheMainVoiceWhileInterrupted_LeavesTheInterruptionPlaying()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);

			var main = state.Play(Runtime, track, CreateSound(), 0f);
			var interrupt = state.Interrupt(Runtime, track, CreateSound(), 0f);

			Runtime.StopVoice(GetVoice(main), 0f);

			Assert.AreEqual(1, state.Count);
			Assert.AreEqual(interrupt, state.GetHandle(0));
			Assert.AreEqual(ThunkVoiceState.Playing, GetVoice(interrupt).State);
		}

		[Test]
		public void NestedInterruptions_UnwindInOrder()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);

			var main = state.Play(Runtime, track, CreateSound(), 0f);
			var first = state.Interrupt(Runtime, track, CreateSound(), 0f);
			var second = state.Interrupt(Runtime, track, CreateSound(), 0f);

			Assert.AreEqual(3, state.Count);
			Assert.AreEqual(ThunkVoiceState.Paused, GetVoice(main).State);
			Assert.AreEqual(ThunkVoiceState.Paused, GetVoice(first).State);

			Runtime.StopVoice(GetVoice(second), 0f);

			Assert.AreEqual(2, state.Count);
			Assert.AreEqual(ThunkVoiceState.Playing, GetVoice(first).State);
			Assert.AreEqual(ThunkVoiceState.Paused, GetVoice(main).State);

			Runtime.StopVoice(GetVoice(first), 0f);

			Assert.AreEqual(1, state.Count);
			Assert.AreEqual(ThunkVoiceState.Playing, GetVoice(main).State);
		}

		[Test]
		public void Stop_EmptiesTheTrackAndFreesEveryVoice()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);
			var mainSound = CreateSound();
			var interruptSound = CreateSound();

			var main = state.Play(Runtime, track, mainSound, 0f);
			var interrupt = state.Interrupt(Runtime, track, interruptSound, 0f);

			state.Stop(Runtime, 0f);

			Assert.AreEqual(0, state.Count);
			Assert.IsFalse(Resolves(main));
			Assert.IsFalse(Resolves(interrupt));
			Assert.AreEqual(0, Runtime.GetRecord(mainSound).LiveCount);
			Assert.AreEqual(0, Runtime.GetRecord(interruptSound).LiveCount);
		}
	}
}
