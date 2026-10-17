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
			if (!TryGetComponent<LayoutItem>(out var layoutItem)) {
				if (string.IsNullOrEmpty(widthGroup)) {
					return this;
				}

				layoutItem = gameObject.AddComponent<LayoutItem>();
			}

			layoutItem.WidthGroupName = widthGroup;
			return this;
		}
	}
}