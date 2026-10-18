using System;
using System.Collections.Generic;
using System.Threading;

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
	/// Handed to a command that takes one as a parameter, for output beyond its return value, and for knowing when
	/// a long-running command has been cancelled.
	/// </summary>
	public sealed class CommandContext
	{
		/// <summary>
		/// The line that ran the command, as typed. For one step of a "a; b" line, just that step.
		/// </summary>
		public string Line { get; }

		/// <summary>
		/// Cancelled when whoever ran the command wants it stopped, e.g. Escape in the console. An async command
		/// checks it between steps or passes it on; what's already happened stays done.
		/// </summary>
		public CancellationToken CancellationToken { get; }

		internal List<CommandLine> Output { get; } = new();

		/// <summary>
		/// True once Error has been called. The command still finishes; it's reported as failed.
		/// </summary>
		public bool Failed { get; private set; }

		// Told about each line as it's written, so a console can show an async command's output as it happens
		private readonly Action<CommandLine> m_written;

		internal CommandContext(string line, Action<CommandLine> written = null, CancellationToken cancellationToken = default)
		{
			Line = line;
			CancellationToken = cancellationToken;
			m_written = written;
		}

		public void Log(string text)
		{
			Write(new CommandLine(text, false));
		}

		public void Error(string text)
		{
			Failed = true;
			Write(new CommandLine(text, true));
		}

		private void Write(CommandLine line)
		{
			Output.Add(line);
			m_written?.Invoke(line);
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
