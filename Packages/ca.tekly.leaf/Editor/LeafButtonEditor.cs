using Tekly.Leaf;
using Tekly.Leaf.Elements;
using UnityEditor;
using UnityEditor.UI;

namespace TeklySample.Editor
{
	[CustomEditor(typeof(LeafButton), true)]
	[CanEditMultipleObjects]
	public class LeafButtonEditor : ButtonEditor
	{
		private SerializedProperty m_animatorProperty;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_animatorProperty = serializedObject.FindProperty("m_animator");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.Space();

			serializedObject.Update();
			EditorGUILayout.PropertyField(m_animatorProperty);
			serializedObject.ApplyModifiedProperties();
		}
	}
}