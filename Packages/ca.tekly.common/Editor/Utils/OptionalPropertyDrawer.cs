using System;
using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Utils
{
	[CustomPropertyDrawer(typeof(OptionalBool))]
	[CustomPropertyDrawer(typeof(OptionalInt))]
	[CustomPropertyDrawer(typeof(OptionalFloat))]
	[CustomPropertyDrawer(typeof(OptionalDouble))]
	[CustomPropertyDrawer(typeof(OptionalString))]
	public class OptionalPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);
			position = EditorGUI.PrefixLabel(position, label);

			EditorGUI.BeginChangeCheck();

			var isSet = property.FindPropertyRelative("IsSet");
			var value = property.FindPropertyRelative("Value");

			var toggleRect = new Rect(position);
			toggleRect.width = 16;
			position.xMin = toggleRect.xMax;
			
			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			var oldValue = isSet.boolValue;
			isSet.boolValue = EditorGUI.Toggle(toggleRect, isSet.boolValue);

			if (oldValue && !isSet.boolValue) {
				ResetValue(value);
			}

			GUI.enabled = isSet.boolValue;
			EditorGUI.PropertyField(position, value, GUIContent.none);
			GUI.enabled = true;
			if (EditorGUI.EndChangeCheck()) {
				property.serializedObject.ApplyModifiedProperties();
			}

			EditorGUI.indentLevel = indent;
			EditorGUI.EndProperty();
		}

		private static void ResetValue(SerializedProperty property)
		{
			switch (property.propertyType) {
				case SerializedPropertyType.Generic:
					break;
				case SerializedPropertyType.Integer:
					property.intValue = default;
					break;
				case SerializedPropertyType.Boolean:
					property.boolValue = default;
					break;
				case SerializedPropertyType.Float:
					property.floatValue = default;
					break;
				case SerializedPropertyType.String:
					property.stringValue = default;
					break;
				case SerializedPropertyType.Color:
					property.colorValue = default;
					break;
				case SerializedPropertyType.ObjectReference:
					property.objectReferenceValue = default;
					break;
				case SerializedPropertyType.LayerMask:
					property.enumValueIndex = default;
					break;
				case SerializedPropertyType.Enum:
					property.enumValueIndex = default;
					break;
				case SerializedPropertyType.Vector2:
					property.vector2Value = default;
					break;
				case SerializedPropertyType.Vector3:
					property.vector3Value = default;
					break;
				case SerializedPropertyType.Vector4:
					property.vector4Value = default;
					break;
				case SerializedPropertyType.Rect:
					property.rectValue = default;
					break;
				case SerializedPropertyType.ArraySize:
					break;
				case SerializedPropertyType.Character:
					property.intValue = default;
					break;
				case SerializedPropertyType.AnimationCurve:
					property.animationCurveValue = new AnimationCurve();
					break;
				case SerializedPropertyType.Bounds:
					property.boundsValue = default;
					break;
				case SerializedPropertyType.Gradient:
					Debug.LogError("Gradient is an unsupported optional type");
					break;
				case SerializedPropertyType.Quaternion:
					property.quaternionValue = Quaternion.identity;
					break;
				case SerializedPropertyType.ExposedReference:
					property.exposedReferenceValue = default;
					break;
				case SerializedPropertyType.FixedBufferSize:
					Debug.LogError("FixedBufferSize is an unsupported optional type");
					break;
				case SerializedPropertyType.Vector2Int:
					property.vector2IntValue = default;
					break;
				case SerializedPropertyType.Vector3Int:
					property.vector3IntValue = default;
					break;
				case SerializedPropertyType.RectInt:
					property.rectIntValue = default;
					break;
				case SerializedPropertyType.BoundsInt:
					property.boundsIntValue = default;
					break;
				case SerializedPropertyType.ManagedReference:
					property.managedReferenceValue = default;
					break;
				case SerializedPropertyType.Hash128:
					property.hash128Value = default;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}