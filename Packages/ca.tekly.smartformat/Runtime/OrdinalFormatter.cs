using System;
using SmartFormat.Core.Extensions;
using UnityEngine;

namespace Tekly.SmartFormat
{
	public enum GrammaticalGender
	{
		Masculine,
		Feminine
	}

	/// <summary>
	/// "{0:ordinal:}"
	/// "{0:ordinal(m):}"
	/// "{0:ordinal(f):}"
	/// "{0:ordinal(f):}"
	/// </summary>
	public sealed class OrdinalFormatter : IFormatter
	{
		public string Name { get; set; } = "ordinal";
		public bool CanAutoDetect { get; set; } = false;
		public static SystemLanguage Language { get; set; } = SystemLanguage.English;

		public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
		{
			if (formattingInfo.CurrentValue is not int number) {
				return false;
			}

			var gender = ParseGender(formattingInfo.FormatterOptions);
			formattingInfo.Write(ToOrdinal(number, Language, gender));

			return true;
		}

		private static GrammaticalGender ParseGender(string options)
		{
			var value = options.AsSpan().Trim();

			var isFeminine = value.Equals("f", StringComparison.OrdinalIgnoreCase) ||
			                 value.Equals("female", StringComparison.OrdinalIgnoreCase) ||
			                 value.Equals("feminine", StringComparison.OrdinalIgnoreCase);
			
			return isFeminine ? GrammaticalGender.Feminine : GrammaticalGender.Masculine;
		}

		public static string ToOrdinal(int number, SystemLanguage language, GrammaticalGender gender = GrammaticalGender.Masculine)
		{
			return language switch {
				SystemLanguage.English => ToEnglish(number),
				SystemLanguage.French => ToFrench(number, gender),
				SystemLanguage.Italian => ToItalian(number, gender),
				SystemLanguage.German => ToGerman(number),
				SystemLanguage.Spanish => ToSpanish(number, gender),
				_ => number.ToString()
			};
		}

		private static string ToEnglish(int number)
		{
			var abs = Math.Abs(number);
			var lastTwo = abs % 100;

			if (lastTwo is >= 11 and <= 13) {
				return $"{number}th";
			}

			return (abs % 10) switch {
				1 => $"{number}st",
				2 => $"{number}nd",
				3 => $"{number}rd",
				_ => $"{number}th"
			};
		}

		private static string ToFrench(int number, GrammaticalGender gender)
		{
			// 1er = premier, 1re = première
			if (number == 1) {
				return gender == GrammaticalGender.Feminine ? "1re" : "1er";
			}

			// 2e, 3e, 4e...
			return $"{number}e";
		}

		private static string ToItalian(int number, GrammaticalGender gender)
		{
			// 1º, 2º, 3º... masculine
			// 1ª, 2ª, 3ª... feminine
			return $"{number}{(gender == GrammaticalGender.Feminine ? "ª" : "º")}";
		}

		private static string ToGerman(int number)
		{
			// German numeric ordinals use a period:
			// 1. 2. 3. 21. etc.
			return $"{number}.";
		}

		private static string ToSpanish(int number, GrammaticalGender gender)
		{
			// 1.º, 2.º, 3.º... masculine
			// 1.ª, 2.ª, 3.ª... feminine
			return $"{number}.{(gender == GrammaticalGender.Feminine ? "ª" : "º")}";
		}
	}
}