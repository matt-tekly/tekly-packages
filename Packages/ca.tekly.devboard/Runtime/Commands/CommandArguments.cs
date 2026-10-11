using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tekly.DevBoard.Commands
{
	/// <summary>
	/// A word of a typed line and where it is in the line.
	/// </summary>
	public readonly struct Word
	{
		public string Text { get; }
		public int Start { get; }
		public int End { get; }

		public Word(string text, int start, int end)
		{
			Text = text;
			Start = start;
			End = end;
		}
	}

	/// <summary>
	/// Splits a typed line into words and turns words into typed arguments.
	/// </summary>
	public static class CommandArguments
	{
		/// <summary>
		/// The line split on spaces, keeping "quoted text" together as one word without its quotes. \" is a quote
		/// inside quotes.
		/// </summary>
		public static List<string> Split(string line)
		{
			var words = new List<string>();

			foreach (var word in SplitWords(line, out _)) {
				words.Add(word.Text);
			}

			return words;
		}

		/// <summary>
		/// Like Split, but with where each word is in line: Start is its first character (its opening quote, if
		/// quoted) and End is one past its last. open is true when line ends inside quotes.
		/// </summary>
		public static List<Word> SplitWords(string line, out bool open)
		{
			var words = new List<Word>();
			var word = new StringBuilder();
			var quoted = false;
			var start = -1;

			for (var i = 0; i < line.Length; i++) {
				var c = line[i];

				if (quoted) {
					if (c == '\\' && i + 1 < line.Length && line[i + 1] == '"') {
						word.Append('"');
						i++;
					} else if (c == '"') {
						quoted = false;
					} else {
						word.Append(c);
					}
				} else if (c == '"') {
					quoted = true;
					start = start < 0 ? i : start;
				} else if (char.IsWhiteSpace(c)) {
					if (start >= 0) {
						words.Add(new Word(word.ToString(), start, i));
						word.Clear();
						start = -1;
					}
				} else {
					word.Append(c);
					start = start < 0 ? i : start;
				}
			}

			if (start >= 0) {
				words.Add(new Word(word.ToString(), start, line.Length));
			}

			open = quoted;
			return words;
		}

		/// <summary>
		/// A value as typed for a word: quoted when it holds spaces or quotes.
		/// </summary>
		public static string Quote(string value)
		{
			if (value.Length > 0 && value.IndexOf(' ') < 0 && value.IndexOf('"') < 0) {
				return value;
			}

			return "\"" + value.Replace("\"", "\\\"") + "\"";
		}

		/// <summary>
		/// Converts a typed word to type. Numbers are read the same in every culture. Bools take true/false,
		/// on/off, yes/no and 1/0. Enums take a name, or the start of exactly one name, ignoring case.
		/// </summary>
		public static bool TryConvert(string text, Type type, out object value, out string error)
		{
			value = null;
			error = null;

			try {
				if (type == typeof(string)) {
					value = text;
					return true;
				}

				if (type == typeof(bool)) {
					switch (text.ToLowerInvariant()) {
						case "true": case "on": case "yes": case "1":
							value = true;
							return true;
						case "false": case "off": case "no": case "0":
							value = false;
							return true;
					}
				} else if (type.IsEnum) {
					return TryConvertEnum(text, type, out value, out error);
				} else if (type == typeof(int)) {
					value = int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
					return true;
				} else if (type == typeof(long)) {
					value = long.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
					return true;
				} else if (type == typeof(float)) {
					value = float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
					return true;
				} else if (type == typeof(double)) {
					value = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
					return true;
				} else if (IsConvertible(type)) {
					value = Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
					return true;
				} else {
					error = $"Commands can't take a {TypeName(type)}";
					return false;
				}
			} catch (FormatException) {
			} catch (OverflowException) {
			} catch (InvalidCastException) {
			}

			error = $"'{text}' isn't a {TypeName(type)}";
			return false;
		}

		/// <summary>
		/// How a type is named in hints: "int", "float", "bool", "text", or an enum's name.
		/// </summary>
		public static string TypeName(Type type)
		{
			if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)) {
				return "int";
			}

			if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) {
				return "number";
			}

			if (type == typeof(bool)) {
				return "bool";
			}

			if (type == typeof(string)) {
				return "text";
			}

			return type.Name;
		}

		/// <summary>
		/// A value as it would be typed, e.g. a default in a usage hint.
		/// </summary>
		public static string Format(object value)
		{
			switch (value) {
				case null:
					return "none";
				case string text:
					return text.Length == 0 || text.Contains(" ") ? $"\"{text}\"" : text;
				case bool flag:
					return flag ? "true" : "false";
				case IFormattable formattable:
					return formattable.ToString(null, CultureInfo.InvariantCulture);
				default:
					return value.ToString();
			}
		}

		private static bool TryConvertEnum(string text, Type type, out object value, out string error)
		{
			value = null;
			error = null;

			string match = null;

			foreach (var name in Enum.GetNames(type)) {
				if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase)) {
					value = Enum.Parse(type, name);
					return true;
				}

				if (name.StartsWith(text, StringComparison.OrdinalIgnoreCase)) {
					if (match != null) {
						error = $"'{text}' could be {match} or {name}";
						return false;
					}

					match = name;
				}
			}

			if (match != null) {
				value = Enum.Parse(type, match);
				return true;
			}

			error = $"'{text}' isn't one of {string.Join(", ", Enum.GetNames(type))}";
			return false;
		}

		private static bool IsConvertible(Type type)
		{
			return typeof(IConvertible).IsAssignableFrom(type) && type != typeof(object);
		}
	}
}
