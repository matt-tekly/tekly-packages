using System.Collections.Generic;
using NUnit.Framework;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Thunk.Tests
{
	public class ThunkRecordTests : ThunkTestBase
	{
		[Test]
		public void Cooldown_IsTrueInsideTheWindow()
		{
			var record = new ThunkSoundRecord { LastPlayTime = 10.0 };

			Assert.IsTrue(record.IsOnCooldown(10.01, 0.03f));
		}

		[Test]
		public void Cooldown_IsFalseAfterTheWindow()
		{
			var record = new ThunkSoundRecord { LastPlayTime = 10.0 };

			Assert.IsFalse(record.IsOnCooldown(10.5, 0.03f));
		}

		[Test]
		public void Cooldown_IsFalseWhenZero()
		{
			var record = new ThunkSoundRecord { LastPlayTime = 10.0 };

			Assert.IsFalse(record.IsOnCooldown(10.0, 0f));
		}

		[Test]
		public void IsFull_TreatsZeroAsUnlimited()
		{
			var record = new ThunkSoundRecord { LiveCount = 100 };

			Assert.IsFalse(record.IsFull(0));
		}

		[Test]
		public void IsFull_IsTrueAtTheLimitOnly()
		{
			var record = new ThunkSoundRecord { LiveCount = 3 };

			Assert.IsTrue(record.IsFull(3));
			Assert.IsTrue(record.IsFull(2));
			Assert.IsFalse(record.IsFull(4));
		}

		[Test]
		public void Select_ReturnsNullForAnEmptySet()
		{
			var record = new ThunkSoundRecord();

			Assert.IsNull(record.Select(0, CreateSet(null, RandomSelectMode.Random)));
			Assert.IsNull(record.Select(0, CreateSet(new AudioClip[0], RandomSelectMode.Random)));
		}

		[Test]
		public void Select_ReturnsTheOnlyClip()
		{
			var record = new ThunkSoundRecord();
			var clips = CreateClips(1);
			var set = CreateSet(clips, RandomSelectMode.NonRepeating);

			Assert.AreSame(clips[0], record.Select(0, set));
			Assert.AreSame(clips[0], record.Select(0, set));
		}

		[Test]
		public void Select_Sequential_ReturnsTheClipsInOrder()
		{
			var record = new ThunkSoundRecord();
			var clips = CreateClips(3);
			var set = CreateSet(clips, RandomSelectMode.Sequential);

			for (var index = 0; index < 7; index++) {
				Assert.AreSame(clips[index % 3], record.Select(0, set), "pick " + index);
			}
		}

		[Test]
		public void Select_NonRepeating_NeverPicksTheSameClipTwiceInARow()
		{
			var record = new ThunkSoundRecord();
			var set = CreateSet(CreateClips(3), RandomSelectMode.NonRepeating);
			var previous = record.Select(0, set);

			for (var index = 0; index < 100; index++) {
				var clip = record.Select(0, set);

				Assert.AreNotSame(previous, clip);
				previous = clip;
			}
		}

		[Test]
		public void Select_Exhaustive_PicksEachClipOncePerRound()
		{
			var record = new ThunkSoundRecord();
			var set = CreateSet(CreateClips(4), RandomSelectMode.Exhaustive);

			for (var round = 0; round < 5; round++) {
				var picked = new HashSet<AudioClip>();

				for (var index = 0; index < 4; index++) {
					picked.Add(record.Select(0, set));
				}

				Assert.AreEqual(4, picked.Count, "round " + round);
			}
		}

		[Test]
		public void Select_Exhaustive_DoesNotThrowForMoreThan64Clips()
		{
			var record = new ThunkSoundRecord();
			var set = CreateSet(CreateClips(70), RandomSelectMode.Exhaustive);

			for (var index = 0; index < 200; index++) {
				Assert.IsNotNull(record.Select(0, set));
			}
		}

		[Test]
		public void Select_KeepsWorkingWhenTheSetSizeChanges()
		{
			var record = new ThunkSoundRecord();
			var large = CreateSet(CreateClips(5), RandomSelectMode.Exhaustive);
			var small = CreateSet(CreateClips(2), RandomSelectMode.Exhaustive);

			for (var index = 0; index < 3; index++) {
				CollectionAssert.Contains(large.Clips, record.Select(0, large));
			}

			for (var index = 0; index < 4; index++) {
				CollectionAssert.Contains(small.Clips, record.Select(0, small));
			}

			for (var index = 0; index < 6; index++) {
				CollectionAssert.Contains(large.Clips, record.Select(0, large));
			}
		}

		[Test]
		public void StateMap_SameNonZeroId_SharesState()
		{
			var map = new ThunkStateMap<ThunkSoundRecord>();
			var a = CreateSound(uniqueId: 42UL);
			var b = CreateSound(uniqueId: 42UL);

			Assert.AreSame(map.Get(a), map.Get(b));
		}

		[Test]
		public void StateMap_DifferentIds_GetDifferentState()
		{
			var map = new ThunkStateMap<ThunkSoundRecord>();
			var a = CreateSound(uniqueId: 1UL);
			var b = CreateSound(uniqueId: 2UL);

			Assert.AreNotSame(map.Get(a), map.Get(b));
		}

		[Test]
		public void StateMap_TwoInstancesWithIdZero_GetDifferentState()
		{
			var map = new ThunkStateMap<ThunkSoundRecord>();
			var a = CreateSound();
			var b = CreateSound();

			Assert.AreNotSame(map.Get(a), map.Get(b));
		}

		[Test]
		public void StateMap_SameInstanceWithIdZero_GetsTheSameStateTwice()
		{
			var map = new ThunkStateMap<ThunkSoundRecord>();
			var a = CreateSound();

			Assert.AreSame(map.Get(a), map.Get(a));
		}
	}
}
