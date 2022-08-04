using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Gui
{
    [CreateAssetMenu]
    public class EditorGuiStyles : ScriptableObject
    {
        public GUIStyle LargeContainer;
        public GUIStyle SmallContainer;
        public GUIStyle Heading;

        private static EditorGuiStyles s_instance;
        
        public static EditorGuiStyles Instance
        {
            get {
                if (s_instance == null) {
                    var assetPath = AssetDatabase.FindAssets("t:EditorGuiStyles").Select(AssetDatabase.GUIDToAssetPath).First();
                    s_instance = AssetDatabase.LoadAssetAtPath<EditorGuiStyles>(assetPath);
                }

                return s_instance;
            }
        }
    }
}