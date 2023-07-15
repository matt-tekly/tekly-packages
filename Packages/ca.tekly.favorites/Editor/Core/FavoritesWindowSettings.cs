using UnityEngine;

namespace Tekly.Favorites
{
	public class FavoritesWindowSettings : ScriptableObject
	{
		public Color SelectedColor = new Color(.09f, .42f, .7f, 0.76f);
		public Color HoverColor = new Color(.6f, .6f, .6f, 0.2f);
		public Color FocusedColor = new Color(.6f, .6f, .6f, 0.2f);

		public GUIContent WindowTitleContent;
		public GUIContent CollectionContent;
		
		public float Width = 250;
		public float Height = 190;
	}
}