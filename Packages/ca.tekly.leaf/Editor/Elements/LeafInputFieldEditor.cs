using TMPro.EditorUtilities;
using UnityEditor;

namespace Tekly.Leaf.Elements
{
	[CustomEditor(typeof(LeafInputField), true)]
	[CanEditMultipleObjects]
	public class LeafInputFieldEditor : TMP_InputFieldEditor
	{
		private SerializedProperty m_animatorProperty;
		private SerializedProperty m_tabNavigatesProperty;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_animatorProperty = serializedObject.FindProperty("m_animator");
			m_tabNavigatesProperty = serializedObject.FindProperty("m_tabNavigates");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.Space();

			serializedObject.Update();
			EditorGUILayout.PropertyField(m_animatorProperty);
			EditorGUILayout.PropertyField(m_tabNavigatesProperty);
			serializedObject.ApplyModifiedProperties();
		}
	}
}
