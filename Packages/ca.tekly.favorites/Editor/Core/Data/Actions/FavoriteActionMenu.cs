using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.Favorites
{
	[CreateAssetMenu(menuName = "Favorites/Menu Action")]
	public class FavoriteActionMenu : FavoriteActionAsset
	{
		[SerializeField, FormerlySerializedAs("Menu")] private string m_Menu;

		public override void Activate()
		{
			EditorApplication.ExecuteMenuItem(m_Menu);
		}
	}
}