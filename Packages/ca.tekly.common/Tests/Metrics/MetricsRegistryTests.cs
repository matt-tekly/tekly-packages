using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using AssertionException = UnityEngine.Assertions.AssertionException;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Tekly.Common.Metrics
{
	public class MetricsRegistryTests
	{
		[Test]
		public void Record_AggregatesAlongPath()
		{
			var metrics = new MetricsRegistry();

			metrics.Record("player.actions.collect_coin", 5);
			metrics.Record("player.actions.collect_coin", 5);

			var player = metrics.Get("player");
			var actions = metrics.Get("player.actions");
			var collectCoin = metrics.Get("player.actions.collect_coin");

			Assert.AreEqual(2, player.Count);
			Assert.AreEqual(10, player.Amount);

			Assert.AreEqual(2, actions.Count);
			Assert.AreEqual(10, actions.Amount);

			Assert.AreEqual(2, collectCoin.Count);
			Assert.AreEqual(10, collectCoin.Amount);
		}
		
		[Test]
		public void Record_AppliesToNodes()
		{
			var metrics = new MetricsRegistry();

			metrics.Record("player.actions.collect_coin", 5);
			metrics.Record("player.actions.collect_coin", 5);

			var player = metrics.GetNode("player");
			var actions = metrics.GetNode("player.actions");
			var collectCoin = metrics.GetNode("player.actions.collect_coin");

			Assert.AreEqual(2, player.Count);
			Assert.AreEqual(10, player.Amount);

			Assert.AreEqual(2, actions.Count);
			Assert.AreEqual(10, actions.Amount);

			Assert.AreEqual(2, collectCoin.Count);
			Assert.AreEqual(10, collectCoin.Amount);
			
			metrics.Record("player.actions.collect_coin", 5);
			
			Assert.AreEqual(3, player.Count);
			Assert.AreEqual(15, player.Amount);
			
			Assert.AreEqual(3, actions.Count);
			Assert.AreEqual(15, actions.Amount);
			
			Assert.AreEqual(3, collectCoin.Count);
			Assert.AreEqual(15, collectCoin.Amount);
		}

		[Test]
		public void Emit_FiresOnceWithCombinedDelta()
		{
			var metrics = new MetricsRegistry();

			long receivedCountDelta = 0;
			double receivedAmountDelta = 0;

			metrics.Subscribe("player.actions", u => {
				receivedCountDelta = u.CountDelta;
				receivedAmountDelta = u.AmountDelta;
			});

			metrics.Record("player.actions.collect_coin", 2);
			metrics.Record("player.actions.collect_coin", 3);

			// Nothing yet
			Assert.AreEqual(0, receivedCountDelta);
			Assert.AreEqual(0, receivedAmountDelta);

			metrics.EmitModifiedNodes();

			Assert.AreEqual(2, receivedCountDelta);
			Assert.AreEqual(5, receivedAmountDelta);
		}

		[Test]
		public void Emit_EmitsEachModifiedNode()
		{
			var metrics = new MetricsRegistry();

			var actionEmits = 0;
			var coinEmits = 0;

			metrics.Subscribe("player.actions", _ => actionEmits++);
			metrics.Subscribe("player.actions.collect_coin", _ => coinEmits++);

			metrics.Record("player.actions.collect_coin", 1);
			metrics.EmitModifiedNodes();

			Assert.AreEqual(1, actionEmits);
			Assert.AreEqual(1, coinEmits);
		}

		[Test]
		public void Record_DuringEmit_IsDeferredToNextWave()
		{
			var metrics = new MetricsRegistry();

			var emits = 0;

			metrics.Subscribe("player.actions", _ => {
				emits++;
				metrics.Record("player.actions.action_1", 1);
			});

			metrics.Record("player.actions.action_0", 1);

			metrics.EmitModifiedNodes();
			Assert.AreEqual(1, emits);

			metrics.EmitModifiedNodes();
			Assert.AreEqual(2, emits);

			metrics.EmitModifiedNodes();
			Assert.AreEqual(3, emits);
		}

		[Test]
		public void Emit_ResetsDirtyState()
		{
			var metrics = new MetricsRegistry();

			var emits = 0;

			metrics.Subscribe("player.actions", _ => emits++);

			metrics.Record("player.actions.collect_coin", 1);
			metrics.EmitModifiedNodes();
			metrics.EmitModifiedNodes();

			Assert.AreEqual(1, emits);

			metrics.Record("player.actions.collect_coin", 1);
			metrics.EmitModifiedNodes();

			Assert.AreEqual(2, emits);
		}

		[Test]
		public void ConstructFromSave_RestoresStats()
		{
			var original = new MetricsRegistry();

			original.Record("player.actions.collect_coin", 5);
			original.Record("player.actions.collect_coin", 3);
			original.Record("player.actions.jump", 1);
			
			var save = original.ToSave();
		
			var restored = new MetricsRegistry(save);
		
			var coins = restored.Get("player.actions.collect_coin");
			Assert.AreEqual(2, coins.Count);
			Assert.AreEqual(8, coins.Amount);

			var jumps = restored.Get("player.actions.jump");
			Assert.AreEqual(1, jumps.Count);
			Assert.AreEqual(1, jumps.Amount);
		
			var actions = restored.Get("player.actions");
			Assert.AreEqual(3, actions.Count);
			Assert.AreEqual(9, actions.Amount);

			var player = restored.Get("player");
			Assert.AreEqual(3, player.Count);
			Assert.AreEqual(9, player.Amount);
		}

		[TestCase(null)]
		[TestCase("   ")]
		[TestCase("player..run")]
		public void ConstructFromSave_MalformedKey_Throws(string key)
		{
			var save = new MetricsRegistrySave();
			save.Entries.Add(new MetricNodeSave { Key = "player.jump", Count = 2, Amount = 2 });
			save.Entries.Add(new MetricNodeSave { Key = key, Count = 1, Amount = 1 });

			Assert.Throws<ArgumentException>(() => new MetricsRegistry(save));
		}

		[Test]
		public void Emit_ThrowingSubscriber_DoesNotStrandOtherNodes()
		{
			var metrics = new MetricsRegistry();

			var jumpEmits = 0;

			metrics.Subscribe("player.actions.collect_coin", _ => throw new InvalidOperationException("boom"));
			metrics.Subscribe("player.actions.jump", _ => jumpEmits++);

			// collect_coin is recorded first so it is emitted before jump.
			metrics.Record("player.actions.collect_coin", 1);
			metrics.Record("player.actions.jump", 1);

			LogAssert.Expect(LogType.Exception, "InvalidOperationException: boom");
			metrics.EmitModifiedNodes();

			Assert.AreEqual(1, jumpEmits);

			// Both nodes must still emit on later records.
			metrics.Record("player.actions.collect_coin", 1);
			metrics.Record("player.actions.jump", 1);

			LogAssert.Expect(LogType.Exception, "InvalidOperationException: boom");
			metrics.EmitModifiedNodes();

			Assert.AreEqual(2, jumpEmits);
		}

		[Test]
		public void Emit_ReentrantCall_IsIgnored()
		{
			var metrics = new MetricsRegistry();

			var emits = 0;

			metrics.Subscribe("player.actions", _ => {
				emits++;
				metrics.EmitModifiedNodes();
			});

			metrics.Record("player.actions.jump", 1);

			// The nested call trips the re-entrancy assertion, which is expected here.
			LogAssert.ignoreFailingMessages = true;

			try {
				metrics.EmitModifiedNodes();
				Assert.AreEqual(1, emits);

				metrics.Record("player.actions.jump", 1);
				metrics.EmitModifiedNodes();
				Assert.AreEqual(2, emits);
			} finally {
				LogAssert.ignoreFailingMessages = false;
			}
		}

		[Test]
		public void Record_ToNodeWithChildren_Asserts()
		{
			var metrics = new MetricsRegistry();
			metrics.Record("player.actions.jump", 1);

			Assert.Throws<AssertionException>(() => metrics.Record("player.actions", 1));
		}

		[Test]
		public void Record_ChildOfRecordedLeaf_Asserts()
		{
			var metrics = new MetricsRegistry();
			metrics.Record("player.actions", 1);

			Assert.Throws<AssertionException>(() => metrics.Record("player.actions.jump", 1));
		}

		[Test]
		public void MetricKey_WellFormed_KeepsPath()
		{
			const string path = "player.actions.jump";
			var key = new MetricKey(path);

			Assert.AreSame(path, key.Path);
		}

		[Test]
		public void MetricKey_TryGetParent_WalksUpToRoot()
		{
			var key = new MetricKey("player.actions.jump");

			Assert.IsTrue(key.TryGetParent(out var actions));
			Assert.AreEqual(new MetricKey("player.actions"), actions);

			Assert.IsTrue(actions.TryGetParent(out var player));
			Assert.AreEqual(new MetricKey("player"), player);

			Assert.IsFalse(player.TryGetParent(out _));
		}

		[Test]
		public void MetricKey_Append_BuildsChild()
		{
			Assert.AreEqual(new MetricKey("player.jump"), new MetricKey("player").Append("jump"));
			Assert.AreEqual(new MetricKey("player"), default(MetricKey).Append("player"));
			Assert.Throws<ArgumentException>(() => new MetricKey("player").Append(""));
			Assert.Throws<ArgumentException>(() => new MetricKey("player").Append(" jump"));
		}

		/// <summary>
		/// Covers the whole hot path: building a key from a literal, the node lookup, the parent walk, the dirty list
		/// and emitting.
		/// </summary>
		[Test]
		public void RecordAndEmit_ExistingMetric_DoesNotAllocate()
		{
			var metrics = new MetricsRegistry();

			TestDelegate recordAndEmit = () => {
				metrics.Record("player.actions.jump", 1);
				metrics.EmitModifiedNodes();
			};

			// The warm-up call creates the nodes. It also JIT-compiles the delegate and resolves its string literal,
			// both of which allocate under Mono. Emitting each time means the measured call takes the
			// first-record-since-emit path that adds nodes to the dirty list.
			recordAndEmit();

			Assert.That(recordAndEmit, Is.Not.AllocatingGCMemory());
		}

		[Test]
		public void DefaultKey_Throws()
		{
			var metrics = new MetricsRegistry();

			Assert.Throws<ArgumentException>(() => metrics.Record(default, 1));
			Assert.Throws<ArgumentException>(() => metrics.Get(default));
		}

		[TestCase("")]
		[TestCase("   ")]
		[TestCase(".")]
		[TestCase("player..jump")]
		[TestCase(".player.jump")]
		[TestCase("player.jump.")]
		[TestCase("player. jump")]
		[TestCase("player .jump")]
		[TestCase(" player.jump")]
		[TestCase("player.jump ")]
		public void MetricKey_Malformed_Throws(string path)
		{
			Assert.Throws<ArgumentException>(() => new MetricKey(path));
		}

		[Test]
		public void MetricKey_Default_HashesWithoutThrowing()
		{
			Assert.AreEqual(0, default(MetricKey).GetHashCode());
			Assert.IsTrue(default(MetricKey).IsEmpty);
		}
	}
}