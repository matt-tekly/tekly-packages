using NUnit.Framework;

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
	}
}