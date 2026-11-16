using System;
using System.Collections.Generic;
using Tekly.Tweenlines;
using UnityEditor;
using UnityEngine;

namespace Tekly.Wipes
{
    /// <summary>
    /// Draws a WipeStyle showing only the settings its field and repeat use, with presets for the classic
    /// looks and a warning when the style can't draw as set up.
    /// </summary>
    public static class WipeStyleGUI
    {
        private static readonly (string Name, Action<WipeStyle> Apply)[] s_presets = {
            ("Wipe", style => { }),
            ("Diagonal Wipe", style => style.Angle = 45),
            ("Barn Doors", style => style.Mirror = true),
            ("Zig Zag Wipe", style => {
                style.WaveAmplitude = 0.08f;
                style.WaveFrequency = 10;
            }),
            ("Iris", style => {
                style.Field = WipeField.Radial;
                style.Exit = WipeExit.Rewind;
            }),
            ("Diamond Iris", style => {
                style.Field = WipeField.Radial;
                style.RadialShape = WipeRadialShape.Diamond;
                style.Exit = WipeExit.Rewind;
            }),
            ("Star Iris", style => {
                style.Field = WipeField.Radial;
                style.RadialShape = WipeRadialShape.Star;
                style.Spin = 0.25f;
                style.Exit = WipeExit.Rewind;
            }),
            ("Clock", style => style.Field = WipeField.Angular),
            ("Pinwheel", style => {
                style.Field = WipeField.Angular;
                style.Arms = 4;
            }),
            ("Swirl", style => {
                style.Field = WipeField.Angular;
                style.Arms = 3;
                style.Twist = 0.75f;
            }),
            ("Blinds", style => {
                style.Repeat = WipeRepeat.Lanes;
                style.LaneElement = WipeLaneElement.Grow;
                style.LaneWidth = 0.1f;
                style.Spread = 0f;
            }),
            ("Sliding Bars", style => {
                style.Angle = 180;
                style.Repeat = WipeRepeat.Lanes;
                style.LaneAngle = 90;
                style.LaneWidth = 0.2f;
                style.LaneElement = WipeLaneElement.Slide;
                style.Cap = WipeCap.Round;
                style.Spread = 0.4f;
                style.Random = 0.3f;
            }),
            ("Diamond Tiles", style => {
                style.Angle = 45;
                style.Repeat = WipeRepeat.Grid;
                style.GridElement = WipeGridElement.Diamond;
                style.GridSize = 0.12f;
                style.Spread = 0.6f;
            }),
            ("Dots", style => {
                style.Field = WipeField.Radial;
                style.Invert = true;
                style.Repeat = WipeRepeat.Grid;
                style.GridElement = WipeGridElement.Circle;
                style.GridSize = 0.08f;
                style.Diagonal = true;
                style.Spread = 0.6f;
            }),
            ("Random Blocks", style => {
                style.Repeat = WipeRepeat.Grid;
                style.GridElement = WipeGridElement.Square;
                style.GridSize = 0.1f;
                style.Spread = 1f;
                style.Random = 1f;
            }),
            ("Fade", style => style.Field = WipeField.Fade)
        };

        /// <summary>
        /// Draws the style's settings. With a preview, the look is shown above them.
        /// </summary>
        public static void Draw(SerializedProperty style, WipePreview preview = null)
        {
            if (preview != null && !style.serializedObject.isEditingMultipleObjects && style.boxedValue is WipeStyle current) {
                preview.Draw(current);
                EditorGUILayout.Space();
            }

            DrawPresets(style);

            var fieldProperty = style.FindPropertyRelative(nameof(WipeStyle.Field));
            EditorGUILayout.PropertyField(fieldProperty);

            var field = (WipeField) fieldProperty.intValue;

            using (new EditorGUI.IndentLevelScope()) {
                DrawField(style, field);
            }

            // Exit applies however the wipe is driven. A silhouette and a fade can only go back the way
            // they came
            if (field != WipeField.Shape && field != WipeField.Fade) {
                Property(style, nameof(WipeStyle.Exit));
            }

            if (WipeStyle.CanRepeat(field)) {
                DrawRepeat(style);
            }

            if (field != WipeField.Fade) {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Edge", EditorStyles.boldLabel);
                Property(style, nameof(WipeStyle.Softness));

                if (field == WipeField.Linear || field == WipeField.Radial || field == WipeField.Angular) {
                    var repeated = WipeStyle.CanRepeat(field) && style.FindPropertyRelative(nameof(WipeStyle.Repeat)).intValue != (int) WipeRepeat.None;

                    // A wave bends the field's own edge. With a repeat the elements draw the edges, so it
                    // is shown switched off instead of hidden, with the reason.
                    using (new EditorGUI.DisabledScope(repeated)) {
                        var amplitude = Property(style, nameof(WipeStyle.WaveAmplitude));

                        if (amplitude.floatValue > 0f) {
                            using (new EditorGUI.IndentLevelScope()) {
                                Property(style, nameof(WipeStyle.Wave));
                                Property(style, nameof(WipeStyle.WaveFrequency));
                            }
                        }
                    }

                    if (repeated) {
                        EditorGUILayout.HelpBox("A wave only shapes a wipe with no Repeat. With a repeat the elements draw the edges, and Softness still softens them.", MessageType.None);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Colours", EditorStyles.boldLabel);
            Property(style, nameof(WipeStyle.Fill));

            if (field != WipeField.Fade) {
                var layers = Property(style, nameof(WipeStyle.Layers));

                if (layers.arraySize > 0) {
                    Property(style, nameof(WipeStyle.LayerOffset));
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Timing From Code", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Used by Cover and Uncover called from code, and by the preview's Play. A Tweenline clip ignores these and uses its own duration and ease.", MessageType.None);

            Property(style, nameof(WipeStyle.CoverDuration));
            Property(style, nameof(WipeStyle.CoverEase));
            Property(style, nameof(WipeStyle.UncoverDuration));
            Property(style, nameof(WipeStyle.UncoverEase));

            if (!style.serializedObject.isEditingMultipleObjects && style.boxedValue is WipeStyle value) {
                var warning = value.GetWarning();

                if (warning != null) {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }
        }

        private static void DrawField(SerializedProperty style, WipeField field)
        {
            switch (field) {
                case WipeField.Linear:
                    Property(style, nameof(WipeStyle.Angle));
                    Property(style, nameof(WipeStyle.Mirror));
                    break;
                case WipeField.Radial:
                    Property(style, nameof(WipeStyle.Center));
                    var shape = (WipeRadialShape) Property(style, nameof(WipeStyle.RadialShape)).intValue;

                    if (shape == WipeRadialShape.Star) {
                        Property(style, nameof(WipeStyle.Points));
                        Property(style, nameof(WipeStyle.Inner));
                    }

                    if (shape != WipeRadialShape.Circle) {
                        Property(style, nameof(WipeStyle.Rotation));
                        Property(style, nameof(WipeStyle.Spin));
                    }

                    break;
                case WipeField.Angular:
                    Property(style, nameof(WipeStyle.Center));
                    Property(style, nameof(WipeStyle.StartAngle));
                    Property(style, nameof(WipeStyle.Arms));
                    Property(style, nameof(WipeStyle.Twist));
                    Property(style, nameof(WipeStyle.Clockwise));
                    break;
                case WipeField.Ramp:
                    Property(style, nameof(WipeStyle.Ramp));
                    Property(style, nameof(WipeStyle.RampFit));
                    break;
                case WipeField.Shape:
                    Property(style, nameof(WipeStyle.Shape));
                    Property(style, nameof(WipeStyle.Center));
                    Property(style, nameof(WipeStyle.Rotation));
                    Property(style, nameof(WipeStyle.Spin));
                    break;
            }

            if (field != WipeField.Fade) {
                Property(style, nameof(WipeStyle.Invert));
            }
        }

        private static void DrawRepeat(SerializedProperty style)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Repeat", EditorStyles.boldLabel);

            var repeat = (WipeRepeat) Property(style, nameof(WipeStyle.Repeat)).intValue;

            if (repeat == WipeRepeat.None) {
                return;
            }

            EditorGUILayout.HelpBox("With a repeat the field above no longer draws an edge. It sets the order the elements start in, and Spread sets how much that order matters.", MessageType.None);

            using (new EditorGUI.IndentLevelScope()) {
                if (repeat == WipeRepeat.Lanes) {
                    Property(style, nameof(WipeStyle.LaneAngle));
                    Property(style, nameof(WipeStyle.LaneWidth));

                    var element = (WipeLaneElement) Property(style, nameof(WipeStyle.LaneElement)).intValue;

                    if (element == WipeLaneElement.Slide) {
                        Property(style, nameof(WipeStyle.Cap));
                    }

                    Property(style, nameof(WipeStyle.Alternate));
                } else {
                    Property(style, nameof(WipeStyle.GridSize));
                    Property(style, nameof(WipeStyle.Diagonal));
                    Property(style, nameof(WipeStyle.GridElement));
                }

                Property(style, nameof(WipeStyle.Spread));
                Property(style, nameof(WipeStyle.Random));
            }
        }

        /// <summary>
        /// A preset replaces the shape settings and keeps the colours and durations.
        /// </summary>
        private static void DrawPresets(SerializedProperty style)
        {
            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, new GUIContent("Preset", "Replaces the shape settings with a classic look. Colours and durations are kept"));

            if (!EditorGUI.DropdownButton(rect, new GUIContent("Apply..."), FocusType.Keyboard)) {
                return;
            }

            var menu = new GenericMenu();
            var serializedObject = style.serializedObject;
            var path = style.propertyPath;

            foreach (var preset in s_presets) {
                var apply = preset.Apply;

                menu.AddItem(new GUIContent(preset.Name), false, () => {
                    serializedObject.Update();

                    var property = serializedObject.FindProperty(path);
                    var current = property.boxedValue as WipeStyle ?? new WipeStyle();

                    var next = new WipeStyle {
                        Fill = current.Fill,
                        Layers = new List<Color>(current.Layers),
                        LayerOffset = current.LayerOffset,
                        CoverDuration = current.CoverDuration,
                        CoverEase = current.CoverEase,
                        UncoverDuration = current.UncoverDuration,
                        UncoverEase = current.UncoverEase
                    };

                    apply(next);

                    property.boxedValue = next;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.DropDown(rect);
        }

        private static SerializedProperty Property(SerializedProperty style, string name)
        {
            var property = style.FindPropertyRelative(name);
            EditorGUILayout.PropertyField(property, true);
            return property;
        }
    }
}
