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

		public DropdownChoice(string title, string subtitle = null)
		{
			Title = title;
			Subtitle = subtitle;
		}

		public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);
	}
}
