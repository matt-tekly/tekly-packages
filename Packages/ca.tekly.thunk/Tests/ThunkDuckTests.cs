using NUnit.Framework;

namespace Tekly.Thunk.Tests
{
	public class ThunkDuckTests : ThunkTestBase
	{
		private const float TOLERANCE = 0.0001f;

		[Test]
		public void DuckGain_IsOneWithNoRequests()
		{
			var state = Runtime.GetTrack(CreateTrack());

			Assert.AreEqual(1f, state.DuckGain, TOLERANCE);
		}

		[Test]
		public void Duck_SetsTheGainAndTheMusicVoiceTrackGain()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);
			var main = state.Play(Runtime, track, CreateSound(), 0f);

			state.Duck(Runtime, track, 0.4f, ThunkHandle.None);

			Assert.AreEqual(0.4f, state.DuckGain, TOLERANCE);
			Assert.AreEqual(0.4f, GetVoice(main).TrackGain, TOLERANCE);
		}

		[TestCase(0.2f, 0.6f)]
		[TestCase(0.6f, 0.2f)]
		public void TwoRequests_TheLowerLevelWins(float first, float second)
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);

			state.Duck(Runtime, track, first, ThunkHandle.None);
			state.Duck(Runtime, track, second, ThunkHandle.None);

			Assert.AreEqual(0.2f, state.DuckGain, TOLERANCE);
		}

		[Test]
		public void Release_RaisesToTheRemainingRequestAndThenToOne()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);

			var high = state.Duck(Runtime, track, 0.6f, ThunkHandle.None);
			var low = state.Duck(Runtime, track, 0.2f, ThunkHandle.None);

			state.ReleaseDuck(Runtime, low);
			Assert.AreEqual(0.6f, state.DuckGain, TOLERANCE);
			Assert.IsFalse(state.IsDucking(low));
			Assert.IsTrue(state.IsDucking(high));

			state.ReleaseDuck(Runtime, high);
			Assert.AreEqual(1f, state.DuckGain, TOLERANCE);
		}

		[Test]
		public void Release_AnUnknownIdOrTheSameIdTwice_ChangesNothing()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);

			state.Duck(Runtime, track, 0.5f, ThunkHandle.None);
			var low = state.Duck(Runtime, track, 0.3f, ThunkHandle.None);

			state.ReleaseDuck(Runtime, 999);
			Assert.AreEqual(0.3f, state.DuckGain, TOLERANCE);

			state.ReleaseDuck(Runtime, low);
			state.ReleaseDuck(Runtime, low);
			Assert.AreEqual(0.5f, state.DuckGain, TOLERANCE);
		}

		[Test]
		public void MusicStartedWhileDucked_StartsAtTheDuckLevel()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);

			state.Duck(Runtime, track, 0.35f, ThunkHandle.None);
			var main = state.Play(Runtime, track, CreateSound(), 0f);

			Assert.AreEqual(0.35f, GetVoice(main).TrackGain, TOLERANCE);
		}

		[TestCase(true)]
		[TestCase(false)]
		public void HeldMusicBroughtBack_HasTheCurrentDuckGain(bool resumeHeld)
		{
			var track = CreateTrack(resumeHeld);
			var state = Runtime.GetTrack(track);

			state.Play(Runtime, track, CreateSound(), 0f);
			var interrupt = state.Interrupt(Runtime, track, CreateSound(), 0f);

			state.Duck(Runtime, track, 0.4f, ThunkHandle.None);
			Runtime.StopVoice(GetVoice(interrupt), 0f);

			Assert.AreEqual(1, state.Count);
			Assert.AreEqual(0.4f, GetVoice(state.GetHandle(0)).TrackGain, TOLERANCE);
		}

		[Test]
		public void PlayOver_DucksTheMusicButIsNotAnEntryAndIsNotDuckedItself()
		{
			var track = CreateTrack(duckLevel: 0.25f);
			var state = Runtime.GetTrack(track);
			var main = state.Play(Runtime, track, CreateSound(), 0f);

			var over = state.PlayOver(Runtime, track, CreateSound());

			Assert.IsFalse(over.IsNone);
			Assert.IsTrue(Resolves(over));
			Assert.AreEqual(1, state.Count);
			Assert.AreEqual(0.25f, state.DuckGain, TOLERANCE);
			Assert.AreEqual(0.25f, GetVoice(main).TrackGain, TOLERANCE);
			Assert.AreEqual(1f, GetVoice(over).TrackGain, TOLERANCE);
		}

		[Test]
		public void StoppingThePlayOverVoice_ReleasesTheDuck()
		{
			var track = CreateTrack(duckLevel: 0.25f);
			var state = Runtime.GetTrack(track);
			var main = state.Play(Runtime, track, CreateSound(), 0f);
			var over = state.PlayOver(Runtime, track, CreateSound());

			Runtime.StopVoice(GetVoice(over), 0f);

			Assert.AreEqual(1f, state.DuckGain, TOLERANCE);
			Assert.AreEqual(1f, GetVoice(main).TrackGain, TOLERANCE);
		}

		[Test]
		public void PlayOver_WithASoundThatCannotPlay_ReturnsNoneAndDoesNotDuck()
		{
			var track = CreateTrack(duckLevel: 0.25f);
			var state = Runtime.GetTrack(track);

			var over = state.PlayOver(Runtime, track, CreateSound(0));

			Assert.IsTrue(over.IsNone);
			Assert.AreEqual(1f, state.DuckGain, TOLERANCE);
		}

		[Test]
		public void Stop_DoesNotStopAPlayOverVoice()
		{
			var track = CreateTrack();
			var state = Runtime.GetTrack(track);

			var main = state.Play(Runtime, track, CreateSound(), 0f);
			var over = state.PlayOver(Runtime, track, CreateSound());

			state.Stop(Runtime, 0f);

			Assert.AreEqual(0, state.Count);
			Assert.IsFalse(Resolves(main));
			Assert.IsTrue(Resolves(over));
		}

		[Test]
		public void FadedDuck_MovesOnUnscaledTimeToTheLevel()
		{
			var track = CreateTrack(duckFadeDown: 1f, duckFadeBack: 2f);
			var state = Runtime.GetTrack(track);

			state.Duck(Runtime, track, 0.2f, ThunkHandle.None);
			Assert.AreEqual(1f, state.DuckGain, TOLERANCE);

			Runtime.Tick(0f, 0.5f);
			Assert.AreEqual(0.6f, state.DuckGain, TOLERANCE);

			Runtime.Tick(0f, 0.5f);
			Assert.AreEqual(0.2f, state.DuckGain, TOLERANCE);
		}

		[Test]
		public void FadedDuck_IgnoresScaledTime()
		{
			var track = CreateTrack(duckFadeDown: 1f, duckFadeBack: 2f);
			var state = Runtime.GetTrack(track);

			state.Duck(Runtime, track, 0.2f, ThunkHandle.None);
			Runtime.Tick(5f, 0f);

			Assert.AreEqual(1f, state.DuckGain, TOLERANCE);
		}

		[Test]
		public void FadedDuck_TakesTheFadeBackTimeToReturnToOne()
		{
			var track = CreateTrack(duckFadeDown: 1f, duckFadeBack: 2f);
			var state = Runtime.GetTrack(track);

			var id = state.Duck(Runtime, track, 0.2f, ThunkHandle.None);
			Runtime.Tick(0f, 1f);
			Assert.AreEqual(0.2f, state.DuckGain, TOLERANCE);

			state.ReleaseDuck(Runtime, id);
			Runtime.Tick(0f, 1f);
			Assert.AreEqual(0.6f, state.DuckGain, TOLERANCE);

			Runtime.Tick(0f, 1f);
			Assert.AreEqual(1f, state.DuckGain, TOLERANCE);
		}
	}
}
