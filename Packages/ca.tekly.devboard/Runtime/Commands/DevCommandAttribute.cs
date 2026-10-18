// Same as DevBoard.cs: DevBoard is in the editor, and in development builds or ones defining DEVBOARD_ENABLED,
// unless they define DEVBOARD_DISABLED
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD || DEVBOARD_ENABLED) && !DEVBOARD_DISABLED)
#define DEVBOARD_ACTIVE
#endif

using System;

namespace Tekly.DevBoard.Commands
{
	/// <summary>
	/// Makes a method a command, run by typing its name and arguments into the console:
	/// <code>
	/// [DevCommand("time scale", "Sets Time.timeScale")]
	/// private static void TimeScale(float scale = 1f) => Time.timeScale = scale;
	/// </code>
	/// Names are words separated by spaces. Parameters are parsed from the typed text by type (numbers, bools,
	/// strings, enums), optional ones use their defaults, and a last string parameter takes the rest of the line.
	/// A CommandContext parameter isn't typed: it's handed in for logging output. A return value is shown as the
	/// result.
	///
	/// A command that takes time returns a Task, Task&lt;T&gt;, Awaitable, Awaitable&lt;T&gt; or IEnumerator coroutine.
	/// The console waits for it, showing what the context logs as it goes, and Escape cancels its
	/// CommandContext.CancellationToken:
	/// <code>
	/// [DevCommand("scene load", "Loads a scene and waits for it")]
	/// private static async Task Load(CommandContext context, string scene)
	/// {
	///     context.Log($"loading {scene}");
	///     await SceneManager.LoadSceneAsync(scene);
	/// }
	/// </code>
	/// Several commands run one after another when separated by ';': "scene load Arena; time scale 1". The line
	/// stops at the first that fails.
	///
	/// Static methods are found on their own, in assemblies that reference DevBoard, the first time commands are
	/// asked for. Instance methods are commands of a particular object, registered with DevBoard.CommandsFrom while
	/// it's around:
	/// <code>
	/// public class InventoryCommands
	/// {
	///     [DevCommand("item give", "Gives count of an item")]
	///     private string Give(CommandContext context, [Suggest(nameof(ItemIds))] string item, int count = 1) { ... }
	/// }
	/// // on start
	/// DevBoard.CommandsFrom(new InventoryCommands(inventory), owner: this);
	/// // on stop
	/// DevBoard.RemovePages(this);
	/// </code>
	///
	/// In builds with DevBoard it derives from PreserveAttribute, so IL2CPP keeps the method even though nothing
	/// calls it directly. In builds without, it's a plain attribute, so the methods are stripped like any other
	/// unused code.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
#if DEVBOARD_ACTIVE
	public sealed class DevCommandAttribute : UnityEngine.Scripting.PreserveAttribute
#else
	public sealed class DevCommandAttribute : Attribute
#endif
	{
		public string Name { get; }
		public string Description { get; }

		public DevCommandAttribute(string name, string description = null)
		{
			Name = name;
			Description = description;
		}
	}
}
