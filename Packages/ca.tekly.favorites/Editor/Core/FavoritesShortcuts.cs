using UnityEditor;

namespace Tekly.Favorites
{
    public static class FavoritesShortcuts
    {
        [MenuItem("Tools/Favorites %G")]
        private static void ShowWindow()
        {
           FavoritesPopup.Present();
        }
    }
}