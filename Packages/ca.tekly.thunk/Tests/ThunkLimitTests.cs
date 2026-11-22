using NUnit.Framework;

namespace Tekly.Thunk.Tests
{
	public class ThunkLimitTests : ThunkTestBase
	{
		[Test]
		public void MaxInstances_StealingNone_RejectsTheExtraPlay()
		{
			var sound = CreateSound(maxInstances: 1, stealing: ThunkStealing.None);
			var first = Runtime.Play(sound, null);
			var second = Runtime.Play(sound, null);

			Assert.IsTrue(second.IsNone);
			Assert.IsTrue(Resolves(first));
		}

		[Test]
		public void MaxInstances_StealingOldest_EndsTheFirstVoice()
		{
			var sound = CreateSound(maxInstances: 2, stealing: ThunkStealing.Oldest);
			var first = Runtime.Play(sound, null);
			var second = Runtime.Play(sound, null);
			var third = Runtime.Play(sound, null);

			Assert.IsFalse(third.IsNone);
			Assert.IsFalse(Resolves(first));
			Assert.IsTrue(Resolves(second));
			Assert.IsTrue(Resolves(third));
			Assert.AreEqual(2, Runtime.GetRecord(sound).LiveCount);
		}

		[Test]
		public void MaxInstances_StealingQuietest_EndsTheLowestUserVolume()
		{
			var sound = CreateSound(maxInstances: 2, stealing: ThunkStealing.Quietest);
			var first = Runtime.Play(sound, null);
			var second = Runtime.Play(sound, null);

			GetVoice(first).UserVolume = 0.8f;
			GetVoice(second).UserVolume = 0.2f;

			var third = Runtime.Play(sound, null);

			Assert.IsFalse(third.IsNone);
			Assert.IsTrue(Resolves(first));
			Assert.IsFalse(Resolves(second));
			Assert.IsTrue(Resolves(third));
		}

		[Test]
		public void Cooldown_RejectsAnImmediateSecondPlay()
		{
			var sound = CreateSound(cooldown: 100f);
			var first = Runtime.Play(sound, null);
			var second = Runtime.Play(sound, null);

			Assert.IsFalse(first.IsNone);
			Assert.IsTrue(second.IsNone);
		}

		[Test]
		public void SoundsSharingAUniqueId_ShareTheMaxInstancesLimit()
		{
			var a = CreateSound(maxInstances: 1, uniqueId: 7UL);
			var b = CreateSound(maxInstances: 1, uniqueId: 7UL);

			var first = Runtime.Play(a, null);
			var second = Runtime.Play(b, null);

			Assert.IsFalse(first.IsNone);
			Assert.IsTrue(second.IsNone);
			Assert.AreSame(Runtime.GetRecord(a), Runtime.GetRecord(b));
		}
	}
}
