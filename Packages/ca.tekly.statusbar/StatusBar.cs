using System.Linq;
using Tekly.Common.Git;
using Tekly.Common.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.StatusBar
{
    public class StatusBar : VisualElement
    {
        private readonly Label m_label;

        public new class UxmlFactory : UxmlFactory<StatusBar, UxmlTraits> { }

        private static string[] s_defaultCommonStyles = {
            "DefaultCommonLight",
            "DefaultCommonDark"
        };

        private const string ASSET_PATH = "Packages/ca.tekly.statusbar/StatusBar.uxml";
        
        public StatusBar()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ASSET_PATH);
            var root = tree.CloneTree();
            
            // There seems to be a bug with Unity where it doesn't add the
            // default styles this window
            AddCommonStyleSheet(root);
            
            hierarchy.Add(root);

            m_label = root.Q<Label>();
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private static void AddCommonStyleSheet(VisualElement element)
        {
            var index = EditorGUIUtility.isProSkin ? 1 : 0;
            
            var commonStyleName = s_defaultCommonStyles[index];
            var commonStyleSheets = Resources.FindObjectsOfTypeAll<StyleSheet>()
                .Where(x => x.name.StartsWith(commonStyleName))
                .ToArray();
            
            element.styleSheets.Add(commonStyleSheets[0]);
        }
        
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null) {
                return;
            }

            PopulateText();
            EditorApplicationExt.UnityEditorFocusChanged += FocusChanged;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null) {
                return;
            }

            EditorApplicationExt.UnityEditorFocusChanged -= FocusChanged;
        }

        private void FocusChanged(bool obj)
        {
            PopulateText();
        }

        private void PopulateText()
        {
            if (GitUtility.TryFindBranchInfo(out var branchInfo)) {
                m_label.text = $"Branch: [{branchInfo.Branch}]";
            } else {
                m_label.text = "git info not found";
            }
        }
    }
}