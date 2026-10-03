using Tekly.Trellis;

namespace Tekly.DevBoard.Components
{
	public class Widget : DevBoardBehaviour
	{
		public Widget WithWidthGroup(string widthGroup)
		{
			if (TryGetComponent<LayoutItem>(out var layoutItem)) {
				layoutItem.WidthGroupName = widthGroup;
			}
			
			return this;
		}
	}
}