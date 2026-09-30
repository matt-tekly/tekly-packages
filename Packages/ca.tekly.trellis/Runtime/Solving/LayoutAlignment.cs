namespace Tekly.Trellis
{
	/// <summary>
	/// Where items sit along a line when there is spare room. Start is left or top.
	/// Spare room only exists once flexible items have taken what they can.
	/// </summary>
	public enum LayoutAlignment
	{
		Start,
		Center,
		End,

		/// <summary>First item at the start, last at the end, spare room shared between the gaps.</summary>
		SpaceBetween,

		/// <summary>Equal gaps before, between and after the items.</summary>
		SpaceEvenly
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
		/// The spread modes answer as if the items were packed: Space Between as Start, Space Evenly as Center.
		/// </summary>
		public static float Factor(this LayoutAlignment alignment)
		{
			switch (alignment) {
				case LayoutAlignment.Center:
				case LayoutAlignment.SpaceEvenly:
					return 0.5f;
				case LayoutAlignment.End:
					return 1f;
				default:
					return 0f;
			}
		}

		/// <summary>
		/// True for alignments that spread spare room between items instead of placing it in one spot.
		/// </summary>
		public static bool IsSpread(this LayoutAlignment alignment)
		{
			return alignment == LayoutAlignment.SpaceBetween || alignment == LayoutAlignment.SpaceEvenly;
		}

		/// <summary>
		/// The equivalent packed alignment for a cross alignment. Stretch counts as Start.
		/// </summary>
		public static LayoutAlignment ToLayoutAlignment(this CrossAlignment alignment)
		{
			switch (alignment) {
				case CrossAlignment.Center:
					return LayoutAlignment.Center;
				case CrossAlignment.End:
					return LayoutAlignment.End;
				default:
					return LayoutAlignment.Start;
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
