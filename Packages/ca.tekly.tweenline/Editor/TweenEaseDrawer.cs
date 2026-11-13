using UnityEditor;
using UnityEngine;

namespace Tekly.Tweenlines
{
    /// <summary>
    /// Draws the type and direction side by side, the settings for the chosen type below them and a graph of
    /// the ease so tuning Back, Elastic and Bounce is visual.
    /// </summary>
    [CustomPropertyDrawer(typeof(TweenEase))]
    public class TweenEaseDrawer : PropertyDrawer
    {
        private const float DIRECTION_WIDTH = 64;
        private const float PREVIEW_HEIGHT = 44;
        private const int PREVIEW_SAMPLES = 64;

        private static readonly Vector3[] s_points = new Vector3[PREVIEW_SAMPLES + 1];
        private static readonly Color s_previewBackground = new Color(0, 0, 0, 0.2f);
        private static readonly Color s_previewGuide = new Color(1, 1, 1, 0.12f);
        private static readonly Color s_previewLine = new Color(0.45f, 0.7f, 0.95f);

        /// <summary>
        /// A short name for a clip label, like "Out Cubic".
        /// </summary>
        public static string Label(SerializedProperty ease)
        {
            var type = (EaseType) ease.FindPropertyRelative(nameof(TweenEase.Type)).intValue;

            if (!HasDirection(type)) {
                return type.ToString();
            }

            var direction = (EaseDirection) ease.FindPropertyRelative(nameof(TweenEase.Direction)).intValue;
            return $"{direction} {type}";
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var type = (EaseType) property.FindPropertyRelative(nameof(TweenEase.Type)).intValue;
            var line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var height = EditorGUIUtility.singleLineHeight + SettingCount(type) * line;

            if (HasPreview(type)) {
                height += EditorGUIUtility.standardVerticalSpacing + PREVIEW_HEIGHT;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var typeProperty = property.FindPropertyRelative(nameof(TweenEase.Type));
            var directionProperty = property.FindPropertyRelative(nameof(TweenEase.Direction));

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var fields = EditorGUI.PrefixLabel(line, label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var type = (EaseType) typeProperty.intValue;
            var typeRect = HasDirection(type)
                ? new Rect(fields.x, fields.y, fields.width - DIRECTION_WIDTH - 2, fields.height)
                : fields;
            var sideRect = new Rect(typeRect.xMax + 2, fields.y, fields.xMax - typeRect.xMax - 2, fields.height);

            EditorGUI.BeginChangeCheck();
            var newType = (EaseType) EditorGUI.EnumPopup(typeRect, type);

            if (EditorGUI.EndChangeCheck() && newType != type) {
                typeProperty.intValue = (int) newType;
                ResetSettings(property, newType);
                type = newType;
            }

            if (HasDirection(type)) {
                EditorGUI.PropertyField(sideRect, directionProperty, GUIContent.none);
            }

            EditorGUI.indentLevel = indent + 1;

            var settingRect = new Rect(position.x, line.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

            switch (type) {
                case EaseType.Back:
                    DrawSetting(ref settingRect, property, nameof(TweenEase.Overshoot));
                    break;
                case EaseType.Elastic:
                    DrawSetting(ref settingRect, property, nameof(TweenEase.Overshoot));
                    DrawSetting(ref settingRect, property, nameof(TweenEase.Oscillations));
                    break;
                case EaseType.Bounce:
                    DrawSetting(ref settingRect, property, nameof(TweenEase.Bounces));
                    DrawSetting(ref settingRect, property, nameof(TweenEase.Bounciness));
                    break;
            }

            EditorGUI.indentLevel = indent;

            if (HasPreview(type)) {
                var previewRect = new Rect(fields.x, settingRect.y, fields.width, PREVIEW_HEIGHT);
                DrawPreview(previewRect, Read(property));
            }

            EditorGUI.EndProperty();
        }

        private static void DrawSetting(ref Rect rect, SerializedProperty property, string name)
        {
            EditorGUI.PropertyField(rect, property.FindPropertyRelative(name));
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        /// <summary>
        /// Settings mean different things per type, so switching type starts the new one from its defaults.
        /// </summary>
        private static void ResetSettings(SerializedProperty property, EaseType type)
        {
            var defaults = new TweenEase(type, EaseDirection.Out);

            property.FindPropertyRelative(nameof(TweenEase.Overshoot)).floatValue = defaults.Overshoot;
            property.FindPropertyRelative(nameof(TweenEase.Oscillations)).intValue = defaults.Oscillations;
            property.FindPropertyRelative(nameof(TweenEase.Bounces)).intValue = defaults.Bounces;
            property.FindPropertyRelative(nameof(TweenEase.Bounciness)).floatValue = defaults.Bounciness;
        }

        private static TweenEase Read(SerializedProperty property)
        {
            return new TweenEase {
                Type = (EaseType) property.FindPropertyRelative(nameof(TweenEase.Type)).intValue,
                Direction = (EaseDirection) property.FindPropertyRelative(nameof(TweenEase.Direction)).intValue,
                Overshoot = property.FindPropertyRelative(nameof(TweenEase.Overshoot)).floatValue,
                Oscillations = property.FindPropertyRelative(nameof(TweenEase.Oscillations)).intValue,
                Bounces = property.FindPropertyRelative(nameof(TweenEase.Bounces)).intValue,
                Bounciness = property.FindPropertyRelative(nameof(TweenEase.Bounciness)).floatValue
            };
        }

        /// <summary>
        /// Graphs the ease with guides at 0 and 1. The range grows to fit overshoot, like Back and Elastic.
        /// </summary>
        private static void DrawPreview(Rect rect, TweenEase ease)
        {
            if (Event.current.type != EventType.Repaint) {
                return;
            }

            EditorGUI.DrawRect(rect, s_previewBackground);

            var min = 0f;
            var max = 1f;

            for (var i = 0; i <= PREVIEW_SAMPLES; i++) {
                var value = ease.Evaluate(i / (float) PREVIEW_SAMPLES);
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }

            var inner = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
            var range = max - min;

            float ToY(float value) => inner.yMax - (value - min) / range * inner.height;

            EditorGUI.DrawRect(new Rect(inner.x, ToY(0), inner.width, 1), s_previewGuide);
            EditorGUI.DrawRect(new Rect(inner.x, ToY(1), inner.width, 1), s_previewGuide);

            for (var i = 0; i <= PREVIEW_SAMPLES; i++) {
                var t = i / (float) PREVIEW_SAMPLES;
                s_points[i] = new Vector3(inner.x + t * inner.width, ToY(ease.Evaluate(t)), 0);
            }

            var color = Handles.color;
            Handles.color = s_previewLine;
            Handles.DrawAAPolyLine(2, s_points);
            Handles.color = color;
        }

        private static bool HasDirection(EaseType type)
        {
            return type != EaseType.Linear;
        }

        private static bool HasPreview(EaseType type)
        {
            return type != EaseType.Linear;
        }

        private static int SettingCount(EaseType type)
        {
            switch (type) {
                case EaseType.Back:
                    return 1;
                case EaseType.Elastic:
                case EaseType.Bounce:
                    return 2;
                default:
                    return 0;
            }
        }
    }
}
