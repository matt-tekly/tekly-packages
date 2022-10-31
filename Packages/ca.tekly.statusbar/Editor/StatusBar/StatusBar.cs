using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.Favorites
{
    public class StatusBar : VisualElement
    {
        private readonly Label m_label;

        public new class UxmlFactory : UxmlFactory<StatusBar, UxmlTraits> { }

        private static string[] s_defaultCommonStyles = {
            "DefaultCommonLight",
            "DefaultCommonDark"
        };

        private const string ASSET_PATH = "Packages/ca.tekly.statusbar/Editor/StatusBar/StatusBar.uxml";
        public StatusBar()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ASSET_PATH);
            var root = tree.CloneTree();

            var index = EditorGUIUtility.isProSkin ? 1 : 0;
            var commonStyleName = s_defaultCommonStyles[index];
            var commonStyleSheets = Resources.FindObjectsOfTypeAll<StyleSheet>()
                .Where(x => x.name.StartsWith(commonStyleName))
                .ToArray();
            
            root.styleSheets.Add(commonStyleSheets[0]);
            hierarchy.Add(root);

            m_label = root.Q<Label>();
        }
    }
}