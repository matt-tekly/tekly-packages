namespace Tekly.Trellis
{
	/// <summary>
	/// Where items sit along a line when there is spare room. Start is left or top.
	/// </summary>
	public enum LayoutAlignment
	{
		Start,
		Center,
		End
	}

	/// <summary>
	/// How an item is sized and placed across a line. Start is left or top.
	/// Stretch fills the line up to the item's max; the others use the item's preferred size.
	/// Flexible items always stretch.
	/// </summary>
	public enum CrossAlignment
	{
		Stretch,
		Start,
		Center,
		End
	}

	public static class AlignmentExtensions
	{
		/// <summary>
		/// Fraction of spare space placed before the items: 0, 0.5 or 1.
		/// </summary>
		public static float Factor(this LayoutAlignment alignment)
		{
			switch (alignment) {
				case LayoutAlignment.Center:
					return 0.5f;
				case LayoutAlignment.End:
					return 1f;
				default:
					return 0f;
			}
		}

		/// <summary>
		/// Fraction of spare space placed before the item: 0, 0.5 or 1. Stretch counts as Start.
		/// </summary>
		public static float Factor(this CrossAlignment alignment)
		{
			switch (alignment) {
				case CrossAlignment.Center:
					return 0.5f;
				case CrossAlignment.End:
					return 1f;
				default:
					return 0f;
			}
		}
	}
}
