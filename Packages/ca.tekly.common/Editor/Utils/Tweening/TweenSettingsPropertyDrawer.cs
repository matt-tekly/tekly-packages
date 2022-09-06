using Tekly.Common.Gui;
using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Utils.Tweening
{
    [CustomPropertyDrawer(typeof(TweenSettings))]
    public class TweenSettingsPropertyDrawer : PropertyDrawer
    {
        private const float BottomSpacing = 2;

        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            pos.height -= BottomSpacing;
            label = EditorGUI.BeginProperty(pos, label, prop);
            var contentRect = EditorGUI.PrefixLabel(pos, GUIUtility.GetControlID(FocusType.Passive), label);
            var labels = new[] {EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow"), EditorGUIUtility.IconContent("UnityEditor.Graphs.AnimatorControllerTool")};
            var properties = new[] {prop.FindPropertyRelative("Duration"), prop.FindPropertyRelative("Ease")};
            PropertyDrawerUtils.DrawMultiplePropertyFields(contentRect, labels, properties);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) + BottomSpacing;
        }
    }
}