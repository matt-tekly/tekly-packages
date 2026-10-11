namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// What a dropdown shows for one choice. The title is what the dropdown's button shows once it's picked.
	/// Search matches the title and the subtitle.
	/// </summary>
	public struct DropdownChoice
	{
		public string Title;

		/// <summary>
		/// A second line under the title, for options that have one. Null or empty for none.
		/// </summary>
		public string Subtitle;

		/// <summary>
		/// Shows Title and Subtitle as rich text, so tags in them style the text. Off by default, so a "&lt;" in a
		/// name is shown as written; turn it on only for text whose tags are meant, and wrap anything else that
		/// goes in it in &lt;noparse&gt;. The default search then matches the tags too.
		/// </summary>
		public bool IsRichText;

		public DropdownChoice(string title, string subtitle = null, bool isRichText = false)
		{
			Title = title;
			Subtitle = subtitle;
			IsRichText = isRichText;
		}

		public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);
	}
}
