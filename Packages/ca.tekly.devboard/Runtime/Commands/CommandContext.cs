using System.Collections.Generic;

namespace Tekly.DevBoard.Commands
{
	/// <summary>
	/// One line of a command's output.
	/// </summary>
	public readonly struct CommandLine
	{
		public string Text { get; }
		public bool IsError { get; }

		public CommandLine(string text, bool isError)
		{
			Text = text;
			IsError = isError;
		}
	}

	/// <summary>
	/// Handed to a command that takes one as a parameter, for output beyond its return value.
	/// </summary>
	public sealed class CommandContext
	{
		/// <summary>
		/// The line that ran the command, as typed.
		/// </summary>
		public string Line { get; }

		internal List<CommandLine> Output { get; } = new();

		/// <summary>
		/// True once Error has been called. The command still finishes; it's reported as failed.
		/// </summary>
		public bool Failed { get; private set; }

		internal CommandContext(string line)
		{
			Line = line;
		}

		public void Log(string text)
		{
			Output.Add(new CommandLine(text, false));
		}

		public void Error(string text)
		{
			Failed = true;
			Output.Add(new CommandLine(text, true));
		}
	}

	/// <summary>
	/// What running a line produced: whether it worked, and its output, the return value last.
	/// </summary>
	public sealed class CommandResult
	{
		public string Line { get; }
		public bool Succeeded { get; }
		public IReadOnlyList<CommandLine> Output { get; }

		internal CommandResult(string line, bool succeeded, IReadOnlyList<CommandLine> output)
		{
			Line = line;
			Succeeded = succeeded;
			Output = output;
		}

		internal static CommandResult Fail(string line, string error)
		{
			return new CommandResult(line, false, new[] { new CommandLine(error, true) });
		}
	}
}
