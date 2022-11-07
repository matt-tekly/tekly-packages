using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Favorites
{
    [CreateAssetMenu(menuName = "Favorites/Disable Raycast Target")]
    public class FavoriteActionDisableRaycastTarget : FavoriteActionAsset
    {
        public override void Activate()
        {
            if (Selection.gameObjects.Length > 0) {
                var targets = new List<Graphic>();
                
                foreach (var gameObject in Selection.gameObjects) {
                    GatherRaycastTargets(gameObject, targets);
                }
                
                Undo.RecordObjects(targets.ToArray(), "Disable Raycast Targets");
                foreach (var graphic in targets) {
                    graphic.raycastTarget = false;
                }
            }
        }

        public static void GatherRaycastTargets(GameObject gameObject, List<Graphic> graphics)
        {
            gameObject.GetComponentsInChildren(true, graphics);
        }
    }
}