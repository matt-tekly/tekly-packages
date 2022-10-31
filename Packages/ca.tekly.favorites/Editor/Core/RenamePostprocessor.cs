using UnityEditor;

namespace Tekly.Favorites
{
    public class RenamePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (movedAssets.Length > 0) {
                FavoritesData.Instance.OnAssetRenamed();
            }
        }
    }
}