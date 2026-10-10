namespace Tekly.DevBoard.Components
{
	public class DividerWidget : Widget
	{
		
	}

	public static class DividerWidgetFactory
	{
		/// <summary>
		/// A line across the parent's layout axis: horizontal in a vertical container, vertical in a row.
		/// </summary>
		public static DividerWidget Divider(this ContainerWidget parent, WidgetPrefab<DividerWidget> prefab = default)
		{
			var across = parent.Axis == Common.Utils.LayoutAxis.Horizontal ? WidgetPrefabs.DividerVertical : WidgetPrefabs.DividerHorizontal;
			return parent.Create(prefab.Or(across));
		}
	}
}
