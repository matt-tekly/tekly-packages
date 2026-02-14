using System;
using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Maths
{
    [CustomPropertyDrawer(typeof(FloatRange))]
    [CustomPropertyDrawer(typeof(IntRange))]
    public sealed class NumericRangePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, label);

            var minProp = property.FindPropertyRelative("Min");
            var maxProp = property.FindPropertyRelative("Max");

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var limits = GetRangeLimitsAttribute();

            if (limits != null)
            {
                if (minProp.propertyType == SerializedPropertyType.Float && maxProp.propertyType == SerializedPropertyType.Float)
                {
                    DrawFloatRangeWithLimits(position, minProp, maxProp, limits);
                }
                else if (minProp.propertyType == SerializedPropertyType.Integer && maxProp.propertyType == SerializedPropertyType.Integer)
                {
                    DrawIntRangeWithLimits(position, minProp, maxProp, limits);
                }
                else
                {
                    DrawDefaultMinMax(position, minProp, maxProp);
                }
            }
            else
            {
                DrawDefaultMinMax(position, minProp, maxProp);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var limits = GetRangeLimitsAttribute();
            if (limits != null && limits.DrawSlider)
            {
                return (EditorGUIUtility.singleLineHeight * 2.0f) + 2.0f;
            }

            return EditorGUIUtility.singleLineHeight;
        }

        private RangeLimitsAttribute GetRangeLimitsAttribute()
        {
            if (fieldInfo == null)
            {
                return null;
            }

            return (RangeLimitsAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(RangeLimitsAttribute));
        }

        private static void DrawDefaultMinMax(Rect position, SerializedProperty minProp, SerializedProperty maxProp)
        {
            var h = EditorGUIUtility.singleLineHeight;

            var minRect = new Rect(position.x, position.y, position.width * 0.5f - 2.5f, h);
            var maxRect = new Rect(minRect.xMax + 5.0f, position.y, position.width * 0.5f - 2.5f, h);

            EditorGUI.PropertyField(minRect, minProp, GUIContent.none);
            EditorGUI.PropertyField(maxRect, maxProp, GUIContent.none);
        }

        private static void DrawFloatRangeWithLimits(Rect position, SerializedProperty minProp, SerializedProperty maxProp, RangeLimitsAttribute limits)
        {
            var h = EditorGUIUtility.singleLineHeight;

            var minRect = new Rect(position.x, position.y, position.width * 0.5f - 2.5f, h);
            var maxRect = new Rect(minRect.xMax + 5.0f, position.y, position.width * 0.5f - 2.5f, h);

            var minValue = minProp.floatValue;
            var maxValue = maxProp.floatValue;

            ClampFloatRange(ref minValue, ref maxValue, limits.AllowedMin, limits.AllowedMax);

            EditorGUI.BeginChangeCheck();
            minValue = EditorGUI.FloatField(minRect, minValue);
            maxValue = EditorGUI.FloatField(maxRect, maxValue);
            if (EditorGUI.EndChangeCheck())
            {
                ClampFloatRange(ref minValue, ref maxValue, limits.AllowedMin, limits.AllowedMax);
                minProp.floatValue = minValue;
                maxProp.floatValue = maxValue;
            }

            if (limits.DrawSlider)
            {
                var sliderRect = new Rect(position.x, position.y + h + 2.0f, position.width, h);

                EditorGUI.BeginChangeCheck();
                EditorGUI.MinMaxSlider(sliderRect, ref minValue, ref maxValue, limits.AllowedMin, limits.AllowedMax);
                if (EditorGUI.EndChangeCheck())
                {
                    ClampFloatRange(ref minValue, ref maxValue, limits.AllowedMin, limits.AllowedMax);
                    minProp.floatValue = minValue;
                    maxProp.floatValue = maxValue;
                }
            }
        }

        private static void DrawIntRangeWithLimits(Rect position, SerializedProperty minProp, SerializedProperty maxProp, RangeLimitsAttribute limits)
        {
            var h = EditorGUIUtility.singleLineHeight;

            var minRect = new Rect(position.x, position.y, position.width * 0.5f - 2.5f, h);
            var maxRect = new Rect(minRect.xMax + 5.0f, position.y, position.width * 0.5f - 2.5f, h);

            var allowedMin = Mathf.RoundToInt(limits.AllowedMin);
            var allowedMax = Mathf.RoundToInt(limits.AllowedMax);

            var minValue = minProp.intValue;
            var maxValue = maxProp.intValue;

            ClampIntRange(ref minValue, ref maxValue, allowedMin, allowedMax);

            EditorGUI.BeginChangeCheck();
            minValue = EditorGUI.IntField(minRect, minValue);
            maxValue = EditorGUI.IntField(maxRect, maxValue);
            if (EditorGUI.EndChangeCheck())
            {
                ClampIntRange(ref minValue, ref maxValue, allowedMin, allowedMax);
                minProp.intValue = minValue;
                maxProp.intValue = maxValue;
            }

            if (limits.DrawSlider)
            {
                var sliderRect = new Rect(position.x, position.y + h + 2.0f, position.width, h);

                var minF = (float)minValue;
                var maxF = (float)maxValue;

                EditorGUI.BeginChangeCheck();
                EditorGUI.MinMaxSlider(sliderRect, ref minF, ref maxF, allowedMin, allowedMax);
                if (EditorGUI.EndChangeCheck())
                {
                    minValue = Mathf.RoundToInt(minF);
                    maxValue = Mathf.RoundToInt(maxF);

                    ClampIntRange(ref minValue, ref maxValue, allowedMin, allowedMax);
                    minProp.intValue = minValue;
                    maxProp.intValue = maxValue;
                }
            }
        }

        private static void ClampFloatRange(ref float minValue, ref float maxValue, float allowedMin, float allowedMax)
        {
            if (allowedMax < allowedMin)
            {
                (allowedMin, allowedMax) = (allowedMax, allowedMin);
            }

            minValue = Mathf.Clamp(minValue, allowedMin, allowedMax);
            maxValue = Mathf.Clamp(maxValue, allowedMin, allowedMax);

            if (maxValue < minValue)
            {
                maxValue = minValue;
            }
        }

        private static void ClampIntRange(ref int minValue, ref int maxValue, int allowedMin, int allowedMax)
        {
            if (allowedMax < allowedMin)
            {
                (allowedMin, allowedMax) = (allowedMax, allowedMin);
            }

            minValue = Mathf.Clamp(minValue, allowedMin, allowedMax);
            maxValue = Mathf.Clamp(maxValue, allowedMin, allowedMax);

            if (maxValue < minValue)
            {
                maxValue = minValue;
            }
        }
    }
}
