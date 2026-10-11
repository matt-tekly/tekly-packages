using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Tekly.DevBoard.Commands
{
	/// <summary>
	/// Every command, and running typed lines against them. Commands come from [DevCommand] static methods, found
	/// the first time commands are asked for, and from DevBoard.Command.
	///
	/// A line is a command's name followed by its arguments, separated by spaces: "give gold 100". The command
	/// with the most of its words at the start of the line wins, so "spawn" and "spawn enemy" can both exist.
	/// </summary>
	public sealed class CommandRegistry
	{
		/// <summary>
		/// Every command, in the order registered.
		/// </summary>
		public IReadOnlyList<Command> All {
			get {
				EnsureDiscovered();
				return m_commands;
			}
		}

		/// <summary>
		/// Raised after commands are added or removed.
		/// </summary>
		public event Action Changed;

		private const int MAX_SUGGESTIONS = 50;

		private readonly List<Command> m_commands = new();
		private bool m_discovered;

		/// <summary>
		/// Registers action as a command. Its parameters become the command's arguments; a method group keeps
		/// its optional parameters' defaults, a lambda can't have any.
		/// </summary>
		public Command Add(string name, Delegate action, string description = null)
		{
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}

			return Add(name, action.Method, action.Target, description);
		}

		/// <summary>
		/// Registers method, called on target (null for a static method), as a command.
		/// </summary>
		public Command Add(string name, MethodInfo method, object target, string description = null)
		{
			if (string.IsNullOrWhiteSpace(name)) {
				throw new ArgumentException("Commands need a name", nameof(name));
			}

			var command = new Command(this, name, description, method, target);

			if (Find(command.Name) != null) {
				Debug.LogWarning($"[DevBoard] There's already a command named '{command.Name}'. The newest one runs.");
			}

			m_commands.Add(command);
			Changed?.Invoke();

			return command;
		}

		/// <summary>
		/// The command with exactly this name, ignoring case and extra spaces. The newest wins if several share it.
		/// </summary>
		public Command Find(string name)
		{
			var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			for (var i = m_commands.Count - 1; i >= 0; i--) {
				if (StartsWith(words, m_commands[i].Words) && words.Length == m_commands[i].Words.Length) {
					return m_commands[i];
				}
			}

			return null;
		}

		/// <summary>
		/// The command line starts with, and the words after its name. False if it doesn't start with one.
		/// </summary>
		public bool TryMatch(string line, out Command command, out List<string> arguments)
		{
			var words = CommandArguments.Split(line ?? string.Empty);
			command = LongestMatch(words);
			arguments = null;

			if (command == null) {
				return false;
			}

			arguments = words.GetRange(command.Words.Length, words.Count - command.Words.Length);
			return true;
		}

		/// <summary>
		/// Runs line: finds its command, converts the arguments, and calls it. Never throws: unknown commands, bad
		/// arguments and exceptions from the command come back as a failed result.
		/// </summary>
		public CommandResult Run(string line)
		{
			line = line?.Trim() ?? string.Empty;

			if (!TryMatch(line, out var command, out var words)) {
				return CommandResult.Fail(line, $"No command matches '{line}'");
			}

			if (!TryConvert(command, words, out var arguments, out var error)) {
				return CommandResult.Fail(line, $"{error}. Usage: {command.Usage}");
			}

			var context = new CommandContext(line);

			try {
				var value = command.Invoke(arguments, context);

				if (value != null) {
					context.Output.Add(new CommandLine(value.ToString(), false));
				}
			} catch (TargetInvocationException exception) {
				var inner = exception.InnerException ?? exception;
				Debug.LogException(inner);
				context.Error($"{inner.GetType().Name}: {inner.Message}");
			} catch (Exception exception) {
				context.Error($"{exception.GetType().Name}: {exception.Message}");
			}

			return new CommandResult(line, !context.Failed, context.Output);
		}

		/// <summary>
		/// What to offer for line as it's typed, with the caret at caret: commands whose names start with what's
		/// typed (word by word, so "inv re" offers "inventory remove"), and once a name is complete, values for the
		/// argument at the caret, from the parameter's suggestions. Also checks the arguments typed so far.
		/// </summary>
		public CommandCompletion Complete(string line, int caret)
		{
			line ??= string.Empty;
			caret = Mathf.Clamp(caret, 0, line.Length);

			var words = CommandArguments.SplitWords(line.Substring(0, caret), out var open);

			// The word the caret is in, or a new empty one when the caret is after a space
			var atSpace = !open && (caret == 0 || char.IsWhiteSpace(line[caret - 1]));
			Word current;

			if (atSpace || words.Count == 0) {
				current = new Word(string.Empty, caret, caret);
			} else {
				current = words[words.Count - 1];
				words.RemoveAt(words.Count - 1);
			}

			// Whatever follows the caret in the same word is replaced too
			var end = caret;

			while (end < line.Length && !char.IsWhiteSpace(line[end])) {
				end++;
			}

			var done = new List<string>(words.Count);

			foreach (var word in words) {
				done.Add(word.Text);
			}

			var command = LongestMatch(done);
			var suggestions = new List<CommandSuggestion>();
			string error = null;
			var argumentIndex = -1;

			if (command != null) {
				argumentIndex = done.Count - command.Words.Length;
				error = CheckArguments(command, done, argumentIndex);

				var parameter = command.ParameterAt(argumentIndex);

				if (parameter != null) {
					var continues = argumentIndex + 1 < command.Parameters.Count;
					AddValues(suggestions, parameter, current.Text, current.Start, end - current.Start, continues);
				} else if (error == null) {
					error = command.Parameters.Count == 0 ? $"'{command.Name}' doesn't take arguments" : "Too many arguments";
				}
			}

			// Names: also offered after a complete name, for longer commands that start with it, like "spawn enemy"
			// after "spawn"
			var nameStart = words.Count > 0 ? words[0].Start : current.Start;
			done.Add(current.Text);
			AddNames(suggestions, done, nameStart, end - nameStart, command);

			if (command == null && suggestions.Count == 0 && line.Trim().Length > 0) {
				error = $"No command starts with '{line.Trim()}'";
			}

			return new CommandCompletion(suggestions, command, error, argumentIndex);
		}

		/// <summary>
		/// The command with the most of its words exactly at the start of words. Null if none.
		/// </summary>
		private Command LongestMatch(IReadOnlyList<string> words)
		{
			Command match = null;

			foreach (var candidate in All) {
				if (StartsWith(words, candidate.Words) && (match == null || candidate.Words.Length >= match.Words.Length)) {
					match = candidate;
				}
			}

			return match;
		}

		/// <summary>
		/// The first problem with the arguments typed before the one being typed, or null.
		/// </summary>
		private static string CheckArguments(Command command, IReadOnlyList<string> words, int typedCount)
		{
			for (var i = 0; i < typedCount; i++) {
				var parameter = command.ParameterAt(i);

				if (parameter == null) {
					return command.Parameters.Count == 0 ? $"'{command.Name}' doesn't take arguments" : "Too many arguments";
				}

				var word = words[command.Words.Length + i];

				if (!CommandArguments.TryConvert(word, parameter.Type, out _, out var conversionError)) {
					return $"{parameter.Name}: {conversionError}";
				}
			}

			return null;
		}

		/// <summary>
		/// The parameter's suggested values that start with typed, then those that contain it.
		/// </summary>
		private static void AddValues(List<CommandSuggestion> suggestions, CommandParameter parameter, string typed, int start, int length,
			bool continues)
		{
			var values = parameter.GetSuggestions();

			if (values == null) {
				return;
			}

			var containing = new List<string>();

			foreach (var value in values) {
				if (suggestions.Count >= MAX_SUGGESTIONS) {
					break;
				}

				if (value.StartsWith(typed, StringComparison.OrdinalIgnoreCase)) {
					suggestions.Add(Value(value));
				} else if (value.IndexOf(typed, StringComparison.OrdinalIgnoreCase) >= 0) {
					containing.Add(value);
				}
			}

			foreach (var value in containing) {
				if (suggestions.Count >= MAX_SUGGESTIONS) {
					break;
				}

				suggestions.Add(Value(value));
			}

			CommandSuggestion Value(string value)
			{
				return new CommandSuggestion(CommandArguments.Quote(value), value, null, start, length, continues);
			}
		}

		/// <summary>
		/// Commands with a word for every typed word, each starting with it, shortest names first. skip is left out,
		/// being the command already typed.
		/// </summary>
		private void AddNames(List<CommandSuggestion> suggestions, List<string> typed, int start, int length, Command skip)
		{
			var names = new List<Command>();

			foreach (var command in All) {
				if (command != skip && StartsWithEach(command.Words, typed)) {
					names.Add(command);
				}
			}

			names.Sort((a, b) => a.Words.Length != b.Words.Length
				? a.Words.Length.CompareTo(b.Words.Length)
				: string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

			foreach (var command in names) {
				if (suggestions.Count >= MAX_SUGGESTIONS) {
					break;
				}

				suggestions.Add(new CommandSuggestion(command.Name, command.Name, command.ArgumentsUsage, start, length,
					command.Parameters.Count > 0, command.Description));
			}
		}

		/// <summary>
		/// Whether each typed word starts the name's word in the same place. Empty typed words match anything.
		/// </summary>
		private static bool StartsWithEach(string[] name, List<string> typed)
		{
			for (var i = 0; i < typed.Count; i++) {
				if (typed[i].Length == 0) {
					continue;
				}

				if (i >= name.Length || !name[i].StartsWith(typed[i], StringComparison.OrdinalIgnoreCase)) {
					return false;
				}
			}

			return true;
		}

		internal void Remove(Command command)
		{
			if (m_commands.Remove(command)) {
				Changed?.Invoke();
			}
		}

		/// <summary>
		/// One argument per parameter: typed ones converted, missing optional ones their defaults. A last text
		/// parameter takes the rest of the line, so "say hello there" needs no quotes.
		/// </summary>
		private static bool TryConvert(Command command, List<string> words, out object[] arguments, out string error)
		{
			var parameters = command.Parameters;
			arguments = new object[parameters.Count];
			error = null;

			if (parameters.Count > 0 && parameters[parameters.Count - 1].Type == typeof(string) && words.Count > parameters.Count) {
				var rest = string.Join(" ", words.GetRange(parameters.Count - 1, words.Count - parameters.Count + 1));
				words = words.GetRange(0, parameters.Count - 1);
				words.Add(rest);
			}

			if (words.Count > parameters.Count) {
				error = parameters.Count == 0 ? $"'{command.Name}' doesn't take arguments" : "Too many arguments";
				return false;
			}

			for (var i = 0; i < parameters.Count; i++) {
				var parameter = parameters[i];

				if (i >= words.Count) {
					if (!parameter.IsOptional) {
						error = $"Missing {parameter}";
						return false;
					}

					arguments[i] = parameter.DefaultValue;
					continue;
				}

				if (!CommandArguments.TryConvert(words[i], parameter.Type, out arguments[i], out var conversionError)) {
					error = $"{parameter.Name}: {conversionError}";
					return false;
				}
			}

			return true;
		}

		private static bool StartsWith(IReadOnlyList<string> words, string[] prefix)
		{
			if (prefix.Length > words.Count) {
				return false;
			}

			for (var i = 0; i < prefix.Length; i++) {
				if (!string.Equals(words[i], prefix[i], StringComparison.OrdinalIgnoreCase)) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Registers every [DevCommand] static method, once. In the editor Unity's TypeCache finds them quickly;
		/// in players the assemblies that reference DevBoard are searched.
		/// </summary>
		private void EnsureDiscovered()
		{
			if (m_discovered) {
				return;
			}

			m_discovered = true;

#if UNITY_EDITOR
			foreach (var method in UnityEditor.TypeCache.GetMethodsWithAttribute<DevCommandAttribute>()) {
				Discover(method);
			}
#else
			var devBoardAssembly = typeof(DevBoard).Assembly;
			var devBoardName = devBoardAssembly.GetName().Name;

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				if (assembly != devBoardAssembly && !References(assembly, devBoardName)) {
					continue;
				}

				foreach (var type in TypesIn(assembly)) {
					foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
						if (method.IsDefined(typeof(DevCommandAttribute), false)) {
							Discover(method);
						}
					}
				}
			}
#endif
		}

		private void Discover(MethodInfo method)
		{
			var attribute = method.GetCustomAttribute<DevCommandAttribute>();

			if (!method.IsStatic) {
				Debug.LogWarning($"[DevBoard] [DevCommand] on {method.DeclaringType?.Name}.{method.Name} is ignored: only static methods " +
					"can be commands. Register instance methods with DevBoard.Command.");
				return;
			}

			try {
				Add(attribute.Name, method, null, attribute.Description);
			} catch (Exception exception) {
				Debug.LogWarning($"[DevBoard] Couldn't register command {method.DeclaringType?.Name}.{method.Name}: {exception.Message}");
			}
		}

#if !UNITY_EDITOR
		private static bool References(Assembly assembly, string name)
		{
			foreach (var reference in assembly.GetReferencedAssemblies()) {
				if (reference.Name == name) {
					return true;
				}
			}

			return false;
		}

		private static IEnumerable<Type> TypesIn(Assembly assembly)
		{
			try {
				return assembly.GetTypes();
			} catch (ReflectionTypeLoadException exception) {
				return Array.FindAll(exception.Types, type => type != null);
			}
		}
#endif
	}
}
