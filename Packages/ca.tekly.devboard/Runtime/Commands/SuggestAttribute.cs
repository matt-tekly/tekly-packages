using System;

namespace Tekly.DevBoard.Commands
{
	/// <summary>
	/// Gives a [DevCommand] parameter the values the console suggests for it, from a static method on the same
	/// type that takes no arguments and returns an IEnumerable (of strings, or anything shown with ToString):
	/// <code>
	/// [DevCommand("give item")]
	/// private static void GiveItem([Suggest(nameof(ItemIds))] string item, int count = 1) { ... }
	/// private static IEnumerable&lt;string&gt; ItemIds() => ItemDatabase.Ids;
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
