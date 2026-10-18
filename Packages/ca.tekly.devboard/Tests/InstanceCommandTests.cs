using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Tekly.DevBoard.Commands;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tekly.DevBoard
{
	[TestFixture]
	public class InstanceCommandTests
	{
		private class Inventory
		{
			public readonly List<string> Ids = new();
			public readonly Dictionary<string, int> Counts = new();
		}

		private class BaseCommands
		{
			public int Pings;

			[DevCommand("test ping", "From a base type")]
			private void Ping() => Pings++;
		}

		private class InventoryCommands : BaseCommands
		{
			public readonly Inventory Inventory;

			public InventoryCommands(Inventory inventory)
			{
				Inventory = inventory;
			}

			[DevCommand("item give", "Gives count of an item")]
			private string Give(CommandContext context, [Suggest(nameof(ItemIds))] string item, int count = 1)
			{
				Inventory.Counts.TryGetValue(item, out var have);
				Inventory.Counts[item] = have + count;
				context.Log($"gave {count} {item}");
				return $"{item} x{Inventory.Counts[item]}";
			}

			[DevCommand("item rarity", "Static [Suggest] still works on an instance command")]
			private void Rarity([Suggest(nameof(Rarities))] string rarity) { }

			private IEnumerable<string> ItemIds() => Inventory.Ids;

			private static IEnumerable<string> Rarities() => new[] { "common", "rare" };
		}

		private CommandRegistry m_registry;

		[SetUp]
		public void SetUp()
		{
			m_registry = new CommandRegistry();
		}

		[Test]
		public void RunsAgainstTheRightTarget()
		{
			var a = new InventoryCommands(new Inventory());
			var b = new InventoryCommands(new Inventory());
			var commandsA = m_registry.AddFrom(a);

			// b registers the same names, so it's newest and runs: the existing duplicate behaviour
			LogAssert.Expect(LogType.Warning, "[DevBoard] There's already a command named 'item give'. The newest one runs.");
			LogAssert.Expect(LogType.Warning, "[DevBoard] There's already a command named 'item rarity'. The newest one runs.");
			LogAssert.Expect(LogType.Warning, "[DevBoard] There's already a command named 'test ping'. The newest one runs.");
			m_registry.AddFrom(b);

			Assert.IsTrue(m_registry.RunAsync("item give sword 3").Result.Succeeded);
			Assert.AreEqual(3, b.Inventory.Counts["sword"]);
			Assert.IsFalse(a.Inventory.Counts.ContainsKey("sword"));

			// Removing b's leaves a's, which then runs
			foreach (var command in commandsA) {
				Assert.IsFalse(command.IsDisposed);
			}

			foreach (var command in m_registry.All.Where(c => !commandsA.Contains(c)).ToList()) {
				command.Dispose();
			}

			Assert.IsTrue(m_registry.RunAsync("item give sword").Result.Succeeded);
			Assert.AreEqual(1, a.Inventory.Counts["sword"]);
			Assert.AreEqual(3, b.Inventory.Counts["sword"]);
		}

		[Test]
		public void DefaultsAndContextWork()
		{
			var target = new InventoryCommands(new Inventory());
			m_registry.AddFrom(target);

			var result = m_registry.RunAsync("item give shield").Result;

			Assert.IsTrue(result.Succeeded);
			Assert.AreEqual(1, target.Inventory.Counts["shield"]);
			Assert.AreEqual("gave 1 shield", result.Output[0].Text);
			Assert.AreEqual("shield x1", result.Output[1].Text);
			Assert.AreEqual("item give <item:text> [count=1]", m_registry.Find("item give").Usage);
		}

		[Test]
		public void FindsInheritedPrivateMethodsAndSkipsStatic()
		{
			var target = new InventoryCommands(new Inventory());
			var commands = m_registry.AddFrom(target);

			CollectionAssert.AreEquivalent(new[] { "item give", "item rarity", "test ping" }, commands.Select(c => c.Name));

			Assert.IsTrue(m_registry.RunAsync("test ping").Result.Succeeded);
			Assert.AreEqual(1, target.Pings);
		}

		[Test]
		public void RemovePagesRemovesAnOwnersCommandsOnly()
		{
			var board = new DevBoard();
			var ownerA = new object();
			var ownerB = new object();
			var a = new InventoryCommands(new Inventory());
			var b = new InventoryCommands(new Inventory());

			board.AddCommands(a, ownerA);
			LogAssert.Expect(LogType.Warning, "[DevBoard] There's already a command named 'item give'. The newest one runs.");
			LogAssert.Expect(LogType.Warning, "[DevBoard] There's already a command named 'item rarity'. The newest one runs.");
			LogAssert.Expect(LogType.Warning, "[DevBoard] There's already a command named 'test ping'. The newest one runs.");
			board.AddCommands(b, ownerB);

			board.ReleaseOwner(ownerB);

			Assert.IsTrue(board.Commands.RunAsync("item give sword").Result.Succeeded);
			Assert.AreEqual(1, a.Inventory.Counts["sword"]);
			Assert.IsFalse(b.Inventory.Counts.ContainsKey("sword"));

			board.ReleaseOwner(ownerA);

			Assert.IsFalse(board.Commands.RunAsync("item give sword").Result.Succeeded);
			Assert.IsNull(board.Commands.Find("item give"));
			Assert.IsNull(board.Commands.Find("test ping"));
		}

		[Test]
		public void DiscoveryIgnoresInstanceMethodsSilently()
		{
			var method = typeof(InventoryCommands).GetMethod("Give", BindingFlags.Instance | BindingFlags.NonPublic);

			m_registry.Discover(method);

			Assert.IsNull(m_registry.Find("item give"));
			LogAssert.NoUnexpectedReceived();
		}

		[Test]
		public void SuggestResolvesInstanceAndStaticMethods()
		{
			var target = new InventoryCommands(new Inventory());
			target.Inventory.Ids.Add("sword");
			target.Inventory.Ids.Add("shield");
			m_registry.AddFrom(target);

			var item = m_registry.Find("item give").Parameters[0];
			CollectionAssert.AreEqual(new[] { "sword", "shield" }, item.GetSuggestions());

			target.Inventory.Ids.Add("bow");
			CollectionAssert.AreEqual(new[] { "sword", "shield", "bow" }, item.GetSuggestions());

			var rarity = m_registry.Find("item rarity").Parameters[0];
			CollectionAssert.AreEqual(new[] { "common", "rare" }, rarity.GetSuggestions());

			var completion = m_registry.Complete("item give s", 11);
			CollectionAssert.AreEqual(new[] { "sword", "shield" }, completion.Suggestions.Select(s => s.Text));
		}
	}
}
