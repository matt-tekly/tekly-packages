//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Tekly.Webster.FramelineCore.Utility
{
	public class ColorHtmlPropertyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(ColorHtmlPropertyAttribute))]
	public class ColorHtmlPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Color currColor;
			ColorUtility.TryParseHtmlString(property.stringValue, out currColor);
			currColor = EditorGUI.ColorField(position, "Color", currColor);
			property.stringValue = string.Format("#{0}", ColorUtility.ToHtmlStringRGBA(currColor));
		}
	}
#endif
}