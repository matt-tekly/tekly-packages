using UnityEditor;

namespace Tekly.Favorites
{
    public static class FavoritesShortcuts
    {
        [MenuItem("Tools/Tekly/Favorites &F")]
        private static void ShowWindow()
        {
           FavoritesPopup.Present();
        }
    }
}