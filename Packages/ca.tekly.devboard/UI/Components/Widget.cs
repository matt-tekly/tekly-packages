using Tekly.Trellis;

namespace Tekly.DevBoard.Components
{
	public class Widget : DevBoardBehaviour
	{
		/// <summary>
		/// Sizes this widget with every other of the same width group under the nearest WidthGroup above: they all
		/// take the widest one's width. Adds a LayoutItem if the widget has none, which only measures its content.
		/// </summary>
		public Widget WithWidthGroup(string widthGroup)
		{
			var layoutItem = GetLayoutItem(!string.IsNullOrEmpty(widthGroup));

			if (layoutItem != null) {
				layoutItem.WidthGroupName = widthGroup;
			}

			return this;
		}

		/// <summary>
		/// How much of a parent layout's spare width this widget takes. 1 fills it: in a row it shares the room left
		/// over, and across a stack it stretches to the full width. Adds a LayoutItem if the widget has none.
		/// </summary>
		public Widget WithFlexibleWidth(float flexible = 1f)
		{
			var layoutItem = GetLayoutItem(flexible != 0f);

			if (layoutItem != null) {
				layoutItem.FlexibleWidth = flexible;
			}

			return this;
		}

		/// <summary>
		/// How much of a parent layout's spare height this widget takes. 1 fills it, see WithFlexibleWidth.
		/// </summary>
		public Widget WithFlexibleHeight(float flexible = 1f)
		{
			var layoutItem = GetLayoutItem(flexible != 0f);

			if (layoutItem != null) {
				layoutItem.FlexibleHeight = flexible;
			}

			return this;
		}

		/// <summary>
		/// The LayoutItem on the widget's own GameObject, which is how its parent layout sizes it. Layouts like
		/// FlowLayout count. Added when there's none and add is true; otherwise null.
		/// </summary>
		private LayoutItem GetLayoutItem(bool add)
		{
			if (TryGetComponent<LayoutItem>(out var layoutItem)) {
				return layoutItem;
			}

			return add ? gameObject.AddComponent<LayoutItem>() : null;
		}
	}
}
