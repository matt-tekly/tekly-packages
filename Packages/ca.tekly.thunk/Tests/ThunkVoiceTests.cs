using NUnit.Framework;

namespace Tekly.Thunk.Tests
{
	public class ThunkVoiceTests : ThunkTestBase
	{
		[Test]
		public void Play_ReturnsAHandleThatResolves()
		{
			var handle = Runtime.Play(CreateSound(), null);

			Assert.IsFalse(handle.IsNone);
			Assert.IsTrue(Resolves(handle));
		}

		[Test]
		public void StopVoice_WithNoFade_StopsTheHandleResolving()
		{
			var handle = Runtime.Play(CreateSound(), null);

			Runtime.StopVoice(GetVoice(handle), 0f);

			Assert.IsFalse(Resolves(handle));
		}

		[Test]
		public void NextVoice_ReusesTheSlotWithADifferentHandle()
		{
			var sound = CreateSound();
			var first = Runtime.Play(sound, null);

			Runtime.StopVoice(GetVoice(first), 0f);

			var second = Runtime.Play(sound, null);

			Assert.AreEqual(first.Index, second.Index);
			Assert.AreNotEqual(first, second);
			Assert.IsFalse(Resolves(first));
			Assert.IsTrue(Resolves(second));
		}

		[Test]
		public void DefaultHandle_NeverResolves()
		{
			Runtime.Play(CreateSound(), null);

			Assert.IsTrue(default(ThunkHandle).IsNone);
			Assert.IsFalse(Resolves(default));
			Assert.IsFalse(Resolves(ThunkHandle.None));
		}

		[Test]
		public void Play_NullSound_ReturnsNone()
		{
			Assert.IsTrue(Runtime.Play(null, null).IsNone);
		}

		[Test]
		public void Play_SoundWithNoClips_ReturnsNone()
		{
			var sound = CreateSound(0);

			Assert.IsTrue(Runtime.Play(sound, null).IsNone);
			Assert.AreEqual(0, Runtime.GetRecord(sound).LiveCount);
		}

		[Test]
		public void LiveCount_GoesUpOnPlayAndDownOnStop()
		{
			var sound = CreateSound();
			var record = Runtime.GetRecord(sound);

			var first = Runtime.Play(sound, null);
			var second = Runtime.Play(sound, null);

			Assert.AreEqual(2, record.LiveCount);

			Runtime.StopVoice(GetVoice(first), 0f);
			Assert.AreEqual(1, record.LiveCount);

			Runtime.StopVoice(GetVoice(second), 0f);
			Assert.AreEqual(0, record.LiveCount);
		}

		[Test]
		public void StopVoice_WithAFade_FreesTheVoiceOnlyAfterTick()
		{
			var handle = Runtime.Play(CreateSound(), null);
			var voice = GetVoice(handle);

			Runtime.StopVoice(voice, 0.5f);

			Assert.IsTrue(Resolves(handle));
			Assert.AreEqual(ThunkVoiceState.Stopping, voice.State);

			Runtime.Tick(1f, 1f);

			Assert.IsFalse(Resolves(handle));
		}

		[Test]
		public void PauseThenResume_ReturnsToPlaying()
		{
			var voice = GetVoice(Runtime.Play(CreateSound(), null));

			Runtime.PauseVoice(voice, 0f);
			Assert.AreEqual(ThunkVoiceState.Paused, voice.State);

			Runtime.ResumeVoice(voice, 0f);
			Assert.AreEqual(ThunkVoiceState.Playing, voice.State);
		}

		[Test]
		public void PauseWithAFade_PausesAfterTick()
		{
			var voice = GetVoice(Runtime.Play(CreateSound(), null));

			Runtime.PauseVoice(voice, 0.5f);
			Assert.AreEqual(ThunkVoiceState.Pausing, voice.State);

			Runtime.Tick(1f, 1f);
			Assert.AreEqual(ThunkVoiceState.Paused, voice.State);
		}

		[Test]
		public void StoppingVoice_IgnoresPauseAndResume()
		{
			var voice = GetVoice(Runtime.Play(CreateSound(), null));

			Runtime.StopVoice(voice, 0.5f);

			Runtime.PauseVoice(voice, 0f);
			Assert.AreEqual(ThunkVoiceState.Stopping, voice.State);

			Runtime.ResumeVoice(voice, 0f);
			Assert.AreEqual(ThunkVoiceState.Stopping, voice.State);
		}

		[Test]
		public void EmitterDestroyed_FreesThatEmittersVoicesOnly()
		{
			var sound = CreateSound();
			var first = CreateEmitter();
			var second = CreateEmitter();

			var onFirst = Runtime.Play(sound, first);
			var onSecond = Runtime.Play(sound, second);

			Runtime.EmitterDestroyed(first);

			Assert.IsFalse(Resolves(onFirst));
			Assert.IsTrue(Resolves(onSecond));
			Assert.AreEqual(1, Runtime.GetRecord(sound).LiveCount);
		}
	}
}
