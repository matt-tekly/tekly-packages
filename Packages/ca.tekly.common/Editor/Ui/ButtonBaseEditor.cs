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
            m_interactable = serializedObject.FindProperty("m_interactable");
            m_onClick = serializedObject.FindProperty("m_onClick");
            m_onClickDisabled = serializedObject.FindProperty("m_onClickDisabled");

            var script = serializedObject.FindProperty("m_Script");

            m_propertyPathToExcludeForChildClasses = new[]
            {
                script.propertyPath,
                m_interactable.propertyPath,
                m_onClick.propertyPath,
                m_onClickDisabled.propertyPath,
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