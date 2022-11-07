using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites
{
    [CreateAssetMenu(menuName = "Favorites/Menu Action")]
    public class FavoriteActionMenu : FavoriteActionAsset
    {
        public string Menu;
        public override void Activate()
        {
            EditorApplication.ExecuteMenuItem(Menu);
        }
    }
}