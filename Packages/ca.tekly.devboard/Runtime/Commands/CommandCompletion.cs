using System.Collections.Generic;

namespace Tekly.DevBoard.Commands
{
	/// <summary>
	/// One thing the console can complete the line with: Text replaces the Length characters at Start.
	/// </summary>
	public readonly struct CommandSuggestion
	{
		/// <summary>
		/// What goes into the line, quoted if it needs to be.
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// What's shown, e.g. a command name or a value.
		/// </summary>
		public string Label { get; }

		/// <summary>
		/// Shown after the label, e.g. a command's arguments. Null for none.
		/// </summary>
		public string Detail { get; }

		/// <summary>
		/// Shown last and quieter, e.g. what a command does. Null for none.
		/// </summary>
		public string Description { get; }

		public int Start { get; }
		public int Length { get; }

		/// <summary>
		/// True when the command has arguments still to type after this, so accepting it adds a space.
		/// </summary>
		public bool Continues { get; }

		public CommandSuggestion(string text, string label, string detail, int start, int length, bool continues,
			string description = null)
		{
			Text = text;
			Label = label;
			Detail = string.IsNullOrEmpty(detail) ? null : detail;
			Description = description;
			Start = start;
			Length = length;
			Continues = continues;
		}

		/// <summary>
		/// The same suggestion for a line that starts offset characters earlier, e.g. the whole line when this was
		/// made for one step of it.
		/// </summary>
		internal CommandSuggestion Shifted(int offset)
		{
			return new CommandSuggestion(Text, Label, Detail, Start + offset, Length, Continues, Description);
		}

		/// <summary>
		/// line with this suggestion in it, and where the caret goes after it.
		/// </summary>
		public string Apply(string line, out int caret)
		{
			var inserted = Continues ? Text + " " : Text;
			caret = Start + inserted.Length;

			var end = Start + Length;
			var rest = end < line.Length ? line.Substring(end) : string.Empty;

			// Don't double up a space that's already there
			if (Continues && rest.StartsWith(" ")) {
				rest = rest.Substring(1);
			}

			return line.Substring(0, Start) + inserted + rest;
		}
	}

	/// <summary>
	/// What the console offers for a line as it's typed: suggestions for the word at the caret, and a hint.
	/// </summary>
	public sealed class CommandCompletion
	{
		public static readonly CommandCompletion Empty = new(new List<CommandSuggestion>(), null, null);

		/// <summary>
		/// Best first. At most CommandRegistry.MAX_SUGGESTIONS of the Total that matched.
		/// </summary>
		public IReadOnlyList<CommandSuggestion> Suggestions { get; }

		/// <summary>
		/// How many suggestions matched, counting those past the limit that weren't built.
		/// </summary>
		public int Total { get; }

		/// <summary>
		/// The command being typed, once its name is complete. Null before.
		/// </summary>
		public Command Command { get; }

		/// <summary>
		/// The argument the caret is on, counting from 0 after the name, or -1 on the name.
		/// </summary>
		public int ArgumentIndex { get; }

		/// <summary>
		/// A problem with the line as typed so far, e.g. a word that isn't a number where one goes. Null when fine.
		/// </summary>
		public string Error { get; }

		public CommandCompletion(IReadOnlyList<CommandSuggestion> suggestions, Command command, string error, int argumentIndex = -1,
			int total = -1)
		{
			Suggestions = suggestions;
			Command = command;
			Error = error;
			ArgumentIndex = argumentIndex;
			Total = total < 0 ? suggestions.Count : total;
		}
	}
}
