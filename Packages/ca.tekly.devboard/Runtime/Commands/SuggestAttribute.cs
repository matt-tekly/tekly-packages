using System;

namespace Tekly.DevBoard.Commands
{
	/// <summary>
	/// Gives a [DevCommand] parameter the values the console suggests for it, from a method on the same type that
	/// takes no arguments and returns an IEnumerable (of strings, or anything shown with ToString):
	/// <code>
	/// [DevCommand("give item")]
	/// private static void GiveItem([Suggest(nameof(ItemIds))] string item, int count = 1) { ... }
	/// private static IEnumerable&lt;string&gt; ItemIds() => ItemDatabase.Ids;
	/// </code>
	/// On a command registered for an object with DevBoard.CommandsFrom, the method can be an instance method, called
	/// on that object; a static one is used otherwise:
	/// <code>
	/// public class InventoryCommands
	/// {
	///     [DevCommand("item give", "Gives count of an item")]
	///     private string Give(CommandContext context, [Suggest(nameof(ItemIds))] string item, int count = 1) { ... }
	///     private IEnumerable&lt;string&gt; ItemIds() => m_inventory.Ids;
	/// }
	/// </code>
	/// Enums and bools are suggested without it.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class SuggestAttribute : Attribute
	{
		public string MethodName { get; }

		public SuggestAttribute(string methodName)
		{
			MethodName = methodName;
		}
	}
}
