using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Pooling
{
	[CustomPropertyDrawer(typeof(SerializedPool<>))]
	public class SerializedPoolDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.PropertyField(position, property.FindPropertyRelative("m_template"), label);
		}
	}
}