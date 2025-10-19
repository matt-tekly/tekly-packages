using System;
using SmartFormat.Core.Extensions;

namespace Tekly.SmartFormat
{
	public class IndefiniteArticleFormatter : IFormatter
	{
		public string Name { get; set; } = "article";
		public bool CanAutoDetect { get; set; } = false;


		public bool TryEvaluateFormat(IFormattingInfo info)
		{
			var w = (info.CurrentValue?.ToString() ?? "").Trim();
			info.Write(NeedsAn(w) ? "an" : "a");
			return true;
		}

		// TODO: Improve these rules - they are pretty terrible
		private static bool NeedsAn(string w)
		{
			if (string.IsNullOrEmpty(w)) {
				return false;
			}

			// Common vowel-sound exceptions (silent 'h')
			if (Starts(w, "hour") || Starts(w, "honest") || Starts(w, "honor") || Starts(w, "honour") ||
			    Starts(w, "heir")) {
				return true;
			}

			// Common consonant-sound 'u/eu' (yoo-/yur-)
			if (Starts(w, "uni") || Starts(w, "use") || Starts(w, "user") || Starts(w, "euro")) {
				return false;
			}

			// Default: first letter vowel?
			var c = char.ToLowerInvariant(w[0]);
			return c is 'a' or 'e' or 'i' or 'o' or 'u';
		}

		private static bool Starts(string s, string p)
		{
			return s.StartsWith(p, StringComparison.OrdinalIgnoreCase);
		}
	}
}