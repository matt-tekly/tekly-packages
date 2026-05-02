using UnityEditor;
using UnityEditor.UI;

namespace Tekly.Leaf.Elements
{
	[CustomEditor(typeof(LeafSlider), true)]
	[CanEditMultipleObjects]
	public class LeafSliderEditor : SliderEditor
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