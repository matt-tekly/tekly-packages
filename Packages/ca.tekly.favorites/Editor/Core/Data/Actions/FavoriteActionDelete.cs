using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites
{
    [CreateAssetMenu(menuName = "Favorites/Delete Action")]
    public class FavoriteActionDelete : FavoriteActionAsset
    {
        public override void Activate()
        {
            if (Selection.activeGameObject != null) {
                Undo.DestroyObjectImmediate(Selection.activeGameObject);
            }
        }
    }
}