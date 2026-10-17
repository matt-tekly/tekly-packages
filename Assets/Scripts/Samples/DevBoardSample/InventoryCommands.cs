using System;
using System.Collections.Generic;
using Tekly.DevBoard.Commands;

namespace TeklySample.Samples.DevBoardSample
{
	/// <summary>
	/// Console commands for one inventory. Instance [DevCommand] methods are registered together with
	/// DevBoard.CommandsFrom(new InventoryCommands(...), owner) and removed with the owner, so a set of commands
	/// lives as long as the thing it drives. A [Suggest] can name an instance method here, for values from the
	/// object itself.
	/// </summary>
	public class InventoryCommands
	{
		private readonly List<string> m_inventory;
		private readonly IReadOnlyList<string> m_knownItems;

		public InventoryCommands(List<string> inventory, IReadOnlyList<string> knownItems)
		{
			m_inventory = inventory;
			m_knownItems = knownItems;
		}

		// A last text argument takes the rest of the line, so "inventory add Gold Ring" needs no quotes
		[DevCommand("inventory add", "Adds an item to the inventory")]
		private string Add([Suggest(nameof(KnownItems))] string item)
		{
			if (m_inventory.Contains(item)) {
				return $"Already have {item}";
			}

			m_inventory.Add(item);
			return $"Added {item}";
		}

		// A CommandContext parameter isn't typed by the user: it's for output, here reporting a failure
		[DevCommand("inventory remove", "Removes an item from the inventory")]
		private void Remove(CommandContext context, [Suggest(nameof(Held))] string item)
		{
			var index = m_inventory.FindIndex(other => string.Equals(other, item, StringComparison.OrdinalIgnoreCase));

			if (index < 0) {
				context.Error($"No {item} in the inventory");
				return;
			}

			context.Log($"Removed {m_inventory[index]}");
			m_inventory.RemoveAt(index);
		}

		[DevCommand("inventory list", "Lists the inventory")]
		private string List() => string.Join(", ", m_inventory);

		[DevCommand("inventory clear", "Empties the inventory")]
		private void Clear() => m_inventory.Clear();

		// Suggestion providers: instance methods taking nothing and returning an IEnumerable
		private IEnumerable<string> KnownItems() => m_knownItems;

		private IEnumerable<string> Held() => m_inventory;
	}
}
