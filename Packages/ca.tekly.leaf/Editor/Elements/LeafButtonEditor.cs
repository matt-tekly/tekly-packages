using UnityEditor;
using UnityEditor.UI;

namespace Tekly.Leaf.Elements
{
	[CustomEditor(typeof(LeafButton), true)]
	[CanEditMultipleObjects]
	public class LeafButtonEditor : SelectableEditor
	{
		private SerializedProperty m_onClickProperty;
		private SerializedProperty m_animatorProperty;
		private SerializedProperty m_pressDelay;
		
		protected override void OnEnable()
		{
			base.OnEnable();
			m_onClickProperty = serializedObject.FindProperty("m_onClick");
			m_animatorProperty = serializedObject.FindProperty("m_animator");
			m_pressDelay = serializedObject.FindProperty("m_pressDelay");
			
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.Space();

			serializedObject.Update();
			EditorGUILayout.PropertyField(m_onClickProperty);
			EditorGUILayout.PropertyField(m_animatorProperty);
			EditorGUILayout.PropertyField(m_pressDelay);
			serializedObject.ApplyModifiedProperties();
		}
	}
}