using Tekly.Leaf.Elements.Radios;
using UnityEditor;
using UnityEditor.UI;

namespace Tekly.Leaf.Elements
{
	[CustomEditor(typeof(LeafRadioOption), true)]
	[CanEditMultipleObjects]
	public class LeafRadioOptionEditor : SelectableEditor
	{
		private SerializedProperty m_onValueChangedProperty;
		private SerializedProperty m_onClickProperty;
		private SerializedProperty m_onSelectedProperty;
		private SerializedProperty m_animatorProperty;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_onValueChangedProperty = serializedObject.FindProperty("m_onValueChanged");
			m_onClickProperty = serializedObject.FindProperty("m_onClick");
			m_onSelectedProperty = serializedObject.FindProperty("m_onSelected");
			m_animatorProperty = serializedObject.FindProperty("m_animator");
		}

		public override void OnInspectorGUI()
		{
			LeafRadioOptionInspector.DrawIsOn(this);

			base.OnInspectorGUI();
			EditorGUILayout.Space();

			serializedObject.Update();
			EditorGUILayout.PropertyField(m_animatorProperty);
			EditorGUILayout.PropertyField(m_onValueChangedProperty);
			EditorGUILayout.PropertyField(m_onClickProperty);
			EditorGUILayout.PropertyField(m_onSelectedProperty);
			serializedObject.ApplyModifiedProperties();
		}
	}
}
