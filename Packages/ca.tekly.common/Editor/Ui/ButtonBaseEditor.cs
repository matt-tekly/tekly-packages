using UnityEditor;

namespace Tekly.Common.Ui
{
    [CustomEditor(typeof(ButtonBase), true)]
    public class ButtonBaseEditor : Editor
    {
        private SerializedProperty m_interactable;

        private SerializedProperty m_onClick;
        private SerializedProperty m_onClickDisabled;

        private static bool s_showClickEvents;
        private string[] m_propertyPathToExcludeForChildClasses;
        
        protected void OnEnable()
        {
            m_interactable = serializedObject.FindProperty("m_Interactable");
            m_onClick = serializedObject.FindProperty("OnClick");
            m_onClickDisabled = serializedObject.FindProperty("OnClickDisabled");

            var script = serializedObject.FindProperty("m_Script");
            var navigation = serializedObject.FindProperty("m_Navigation");
            var targetGraphicProperty = serializedObject.FindProperty("m_TargetGraphic");
            var transitionProperty = serializedObject.FindProperty("m_Transition");
            var colorBlockProperty = serializedObject.FindProperty("m_Colors");
            var spriteStateProperty = serializedObject.FindProperty("m_SpriteState");
            var animTriggerProperty = serializedObject.FindProperty("m_AnimationTriggers");

            m_propertyPathToExcludeForChildClasses = new[]
            {
                script.propertyPath,
                m_interactable.propertyPath,
                navigation.propertyPath,
                m_onClick.propertyPath,
                m_onClickDisabled.propertyPath,
                transitionProperty.propertyPath,
                colorBlockProperty.propertyPath,
                spriteStateProperty.propertyPath,
                animTriggerProperty.propertyPath,
                targetGraphicProperty.propertyPath,
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(m_interactable);
            
            DrawEvents();
            
            DrawPropertiesExcluding(serializedObject, m_propertyPathToExcludeForChildClasses);
            
            serializedObject.ApplyModifiedProperties();
        }

        protected void DrawEvents()
        {
            s_showClickEvents = EditorGUILayout.Foldout(s_showClickEvents, "Events");

            if (s_showClickEvents) {
                EditorGUILayout.PropertyField(m_onClick);
                EditorGUILayout.PropertyField(m_onClickDisabled);
            }
        }
    }
}