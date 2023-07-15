using UnityEngine;

namespace Tekly.Favorites.Gui
{
	public static class GuiUtils
	{
		public static bool DeleteButton(Rect rect)
		{
			return ButtonColored(rect, "-", Color.red);
		}
		
		public static bool AddButton(Rect rect)
		{
			return ButtonColored(rect, "+", Color.green);
		}

		public static bool ButtonColored(Rect rect, string text, Color color)
		{
			var currentColor = GUI.backgroundColor;
            
			GUI.backgroundColor = color;
			var result = GUI.Button(rect, text);
			GUI.backgroundColor = currentColor;
            
			return result;
		}
	}
}