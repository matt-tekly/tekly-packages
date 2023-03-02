using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.Favorites
{
	[CreateAssetMenu(menuName = "Favorites/Menu Action")]
	public class FavoriteActionMenu : FavoriteActionAsset
	{
		[SerializeField, FormerlySerializedAs("Menu"), FormerlySerializedAs("m_Menu")] private string m_menu;

		public override void Activate()
		{
			EditorApplication.ExecuteMenuItem(m_menu);
		}
	}
}