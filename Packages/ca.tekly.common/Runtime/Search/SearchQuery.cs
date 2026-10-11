using System;
using System.Collections.Generic;

namespace Tekly.Common.Search
{
	/// <summary>
	/// Typed search text, split into terms and matched loosely. Like Unity's hierarchy search, terms are separated
	/// by spaces, can come in any order, and every one must match: "gold cheat" finds "Give Gold" on the Cheats page.
	///
	/// A term matches a name at the start of a word, anywhere in it, as the initials of its words ("gm" for
	/// "God Mode"), or as letters in order from the start of a word ("tgl" for "Toggle"). A term can also match
	/// other text, like a location or description, which counts for least. Word starts include camelCase humps.
	///
	/// Set it again as the text changes rather than making a new one, so typing doesn't allocate.
	/// </summary>
	public sealed class SearchQuery
	{
		public const int NO_MATCH = -1;

		// How much the whole text adds when it is the name, or starts it
		private const int SCORE_EXACT = 100;
		private const int SCORE_PREFIX = 50;

		// How much each term adds, by how well it matched
		private const int SCORE_WORD_START = 15;
		private const int SCORE_SUBSTRING = 10;
		private const int SCORE_ACRONYM = 8;
		private const int SCORE_FUZZY = 3;
		private const int SCORE_ELSEWHERE = 1;

		/// <summary>
		/// The text, trimmed.
		/// </summary>
		public string Text { get; private set; } = string.Empty;

		/// <summary>
		/// The text split on spaces, without repeats.
		/// </summary>
		public IReadOnlyList<string> Terms => m_terms;

		public bool IsEmpty => Text.Length == 0;

		private readonly List<string> m_terms = new();

		public SearchQuery(string text = null)
		{
			Set(text);
		}

		public void Set(string text)
		{
			text = text?.Trim() ?? string.Empty;

			if (text == Text) {
				return;
			}

			Text = text;
			m_terms.Clear();

			foreach (var term in text.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
				if (!m_terms.Exists(t => string.Equals(t, term, StringComparison.OrdinalIgnoreCase))) {
					m_terms.Add(term);
				}
			}
		}

		public bool Matches(string name, string other = null)
		{
			return Score(name, other) != NO_MATCH;
		}

		/// <summary>
		/// How well name, or failing that other, matches: higher is better, 0 for an empty search, and
		/// <see cref="NO_MATCH"/> when a term matches neither.
		/// </summary>
		public int Score(string name, string other = null)
		{
			if (IsEmpty) {
				return 0;
			}

			name ??= string.Empty;

			var score = 0;

			if (string.Equals(name, Text, StringComparison.OrdinalIgnoreCase)) {
				score += SCORE_EXACT;
			} else if (name.StartsWith(Text, StringComparison.OrdinalIgnoreCase)) {
				score += SCORE_PREFIX;
			}

			foreach (var term in m_terms) {
				var termScore = TermScore(name, term);

				if (termScore == 0) {
					if (other == null || other.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0) {
						return NO_MATCH;
					}

					termScore = SCORE_ELSEWHERE;
				}

				score += termScore;
			}

			return score;
		}

		/// <summary>
		/// How well one term matches the name, or 0 when it doesn't.
		/// </summary>
		private static int TermScore(string name, string term)
		{
			var first = name.IndexOf(term, StringComparison.OrdinalIgnoreCase);

			if (first >= 0) {
				for (var at = first; at >= 0; at = name.IndexOf(term, at + 1, StringComparison.OrdinalIgnoreCase)) {
					if (IsWordStart(name, at)) {
						return SCORE_WORD_START;
					}
				}

				return SCORE_SUBSTRING;
			}

			// A single letter in order is too loose to mean anything
			if (term.Length < 2) {
				return 0;
			}

			if (InOrder(name, term, true)) {
				return SCORE_ACRONYM;
			}

			return InOrder(name, term, false) ? SCORE_FUZZY : 0;
		}

		/// <summary>
		/// Whether the term's letters appear in the name in order, the first at the start of a word. With
		/// wordStarts, every letter must be at the start of a word.
		/// </summary>
		private static bool InOrder(string name, string term, bool wordStarts)
		{
			var next = 0;

			for (var i = 0; i < name.Length && next < term.Length; i++) {
				if ((wordStarts || next == 0) && !IsWordStart(name, i)) {
					continue;
				}

				if (char.ToLowerInvariant(name[i]) == char.ToLowerInvariant(term[next])) {
					next++;
				}
			}

			return next == term.Length;
		}

		/// <summary>
		/// The start of the text, after anything that isn't a letter or digit, or a capital after a lowercase letter.
		/// </summary>
		private static bool IsWordStart(string text, int index)
		{
			if (index == 0) {
				return true;
			}

			var previous = text[index - 1];
			return !char.IsLetterOrDigit(previous) || (char.IsUpper(text[index]) && char.IsLower(previous));
		}
	}
}
