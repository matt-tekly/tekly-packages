using UnityEditor;
using UnityEditor.UI;

namespace Tekly.DevBoard.Components
{
	[CustomEditor(typeof(LinearLayoutGroup), true)]
	[CanEditMultipleObjects]
	public class LinearLayoutGroupEditor : HorizontalOrVerticalLayoutGroupEditor
	{
		private SerializedProperty m_axis;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_axis = serializedObject.FindProperty("m_axis");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(m_axis);
			serializedObject.ApplyModifiedProperties();

			base.OnInspectorGUI();
		}
	}
}