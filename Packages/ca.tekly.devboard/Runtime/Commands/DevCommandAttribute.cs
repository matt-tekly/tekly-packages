// Same as DevBoard.cs: DevBoard is in the editor, and in development builds or ones defining DEVBOARD_ENABLED,
// unless they define DEVBOARD_DISABLED
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD || DEVBOARD_ENABLED) && !DEVBOARD_DISABLED)
#define DEVBOARD_ACTIVE
#endif

using System;

namespace Tekly.DevBoard.Commands
{
	/// <summary>
	/// Makes a static method a command, run by typing its name and arguments into the console:
	/// <code>
	/// [DevCommand("time scale", "Sets Time.timeScale")]
	/// private static void TimeScale(float scale = 1f) => Time.timeScale = scale;
	/// </code>
	/// Names are words separated by spaces. Parameters are parsed from the typed text by type (numbers, bools,
	/// strings, enums), optional ones use their defaults, and a last string parameter takes the rest of the line.
	/// A CommandContext parameter isn't typed: it's handed in for logging output. A return value is shown as the
	/// result.
	///
	/// Found in assemblies that reference DevBoard, the first time commands are asked for. In builds with DevBoard
	/// it derives from PreserveAttribute, so IL2CPP keeps the method even though nothing calls it directly. In
	/// builds without, it's a plain attribute, so the methods are stripped like any other unused code.
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
