using System.Text;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// Node ids and paths are slugs, so they can be typed: lowercase letters and digits, with a dash between words.
	/// "Labels and Properties" becomes "labels-and-properties", and a path "sample/controls/sliders/time-scale".
	/// </summary>
	public static class NodePaths
	{
		/// <summary>
		/// text as a slug, without any rich text tags. Null when nothing is left.
		/// </summary>
		public static string Slug(string text)
		{
			if (string.IsNullOrEmpty(text)) {
				return null;
			}

			var builder = new StringBuilder(text.Length);
			var inTag = false;
			var dash = false;

			foreach (var c in text) {
				if (c == '<') {
					inTag = true;
					continue;
				}

				if (inTag) {
					inTag = c != '>';
					continue;
				}

				if (!char.IsLetterOrDigit(c)) {
					dash = builder.Length > 0;
					continue;
				}

				if (dash) {
					builder.Append('-');
					dash = false;
				}

				builder.Append(char.ToLowerInvariant(c));
			}

			return builder.Length > 0 ? builder.ToString() : null;
		}

		/// <summary>
		/// Each part of a slash separated path as a slug: "Game/Economy Cheats" becomes "game/economy-cheats".
		/// </summary>
		public static string SlugPath(string path)
		{
			if (string.IsNullOrEmpty(path)) {
				return string.Empty;
			}

			var builder = new StringBuilder(path.Length);

			foreach (var part in path.Split('/')) {
				var slug = Slug(part);

				if (slug == null) {
					continue;
				}

				if (builder.Length > 0) {
					builder.Append('/');
				}

				builder.Append(slug);
			}

			return builder.ToString();
		}
	}
}
