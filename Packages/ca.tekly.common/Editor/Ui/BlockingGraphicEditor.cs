using UnityEditor;
using UnityEditor.UI;

namespace Tekly.Common.Ui
{
    [CanEditMultipleObjects, CustomEditor(typeof(BlockingGraphic), false)]
    public class BlockingGraphicEditor : GraphicEditor
    {
        public override void OnInspectorGUI ()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Script);
			
            RaycastControlsGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}