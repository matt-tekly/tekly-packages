using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tekly.Tweenlines
{
    [CustomEditor(typeof(Tweenline))]
    public class TweenlineEditor : Editor
    {
        private static bool s_eventsExpanded;

        private SerializedProperty m_playOnEnable;
        private SerializedProperty m_useUnscaledTime;
        private SerializedProperty m_speed;
        private SerializedProperty m_stagger;
        private SerializedProperty m_completed;
        private Tweenline m_tweenline;

        private void OnEnable()
        {
            m_tweenline = (Tweenline) target;
            m_playOnEnable = serializedObject.FindProperty("m_playOnEnable");
            m_useUnscaledTime = serializedObject.FindProperty("m_useUnscaledTime");
            m_speed = serializedObject.FindProperty("m_speed");
            m_stagger = serializedObject.FindProperty("m_stagger");
            m_completed = serializedObject.FindProperty("m_completed");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (TweenlineEditorUtils.HasMissingTypes(m_tweenline)) {
                EditorGUILayout.HelpBox($"Some tracks use classes that no longer exist [{TweenlineEditorUtils.MissingTypeNames(m_tweenline)}]", MessageType.Warning);

                if (GUILayout.Button("Remove Missing Tracks")) {
                    TweenlineEditorUtils.RemoveAllMissingTypes(m_tweenline);
                    serializedObject.Update();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUI.BeginChangeCheck();

            DrawPlayOnEnable();
            EditorGUILayout.PropertyField(m_useUnscaledTime);
            EditorGUILayout.PropertyField(m_speed);
            EditorGUILayout.PropertyField(m_stagger);

            EditorGUILayout.Space();
            DrawAnimations();

            if (GUILayout.Button("Open Timeline", GUILayout.Height(24))) {
                TweenlineWindow.Open(m_tweenline);
            }

            s_eventsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(s_eventsExpanded, "Events");

            if (s_eventsExpanded) {
                EditorGUILayout.PropertyField(m_completed);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
                TweenlineEditorUtils.RefreshPreview(m_tweenline);
            }
        }

        private void DrawPlayOnEnable()
        {
            var names = m_tweenline.Animations.Select(animation => animation.Name).ToList();
            var options = new[] { "None" }.Concat(names).ToArray();
            var index = names.IndexOf(m_playOnEnable.stringValue) + 1;

            var rect = EditorGUILayout.GetControlRect();
            var label = EditorGUI.BeginProperty(rect, new GUIContent(m_playOnEnable.displayName, m_playOnEnable.tooltip), m_playOnEnable);
            var selected = EditorGUI.Popup(rect, label, index, options.Select(x => new GUIContent(x)).ToArray());
            EditorGUI.EndProperty();

            if (selected != index) {
                m_playOnEnable.stringValue = selected == 0 ? string.Empty : names[selected - 1];
            }
        }

        private void DrawAnimations()
        {
            EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);

            if (m_tweenline.Animations.Count == 0) {
                EditorGUILayout.HelpBox("No animations. Open the timeline to add one.", MessageType.Info);
                return;
            }

            for (var i = 0; i < m_tweenline.Animations.Count; i++) {
                var animation = m_tweenline.Animations[i];

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                    // Buttons are placed from the right edge first so the labels can never push them out of view
                    var rect = EditorGUILayout.GetControlRect();
                    var deleteRect = new Rect(rect.xMax - 22, rect.y, 22, rect.height);
                    var right = deleteRect.x - 2;

                    if (GUI.Button(deleteRect, new GUIContent("✕", $"Delete {animation.Name}"), EditorStyles.miniButton)) {
                        TweenlineEditorUtils.DeleteAnimation(m_tweenline, i);
                        serializedObject.Update();
                        GUIUtility.ExitGUI();
                    }

                    if (Application.isPlaying) {
                        var playRect = new Rect(right - 40, rect.y, 40, rect.height);
                        right = playRect.x - 2;

                        if (GUI.Button(playRect, "Play", EditorStyles.miniButton)) {
                            m_tweenline.Play(animation, false);
                        }
                    }

                    var nameWidth = Mathf.Min(EditorGUIUtility.labelWidth, (right - rect.x) * 0.5f);
                    GUI.Label(new Rect(rect.x, rect.y, nameWidth, rect.height), animation.Name);
                    GUI.Label(new Rect(rect.x + nameWidth, rect.y, right - rect.x - nameWidth, rect.height), $"{animation.Tracks.Count} tracks  {animation.Duration:0.##}s  {animation.Wrap}", EditorStyles.miniLabel);
                }
            }
        }
    }
}
