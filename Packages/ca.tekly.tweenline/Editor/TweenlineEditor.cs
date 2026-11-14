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
        private SerializedProperty m_hurrySpeed;
        private SerializedProperty m_completed;
        private Tweenline m_tweenline;

        private void OnEnable()
        {
            m_tweenline = (Tweenline) target;
            m_playOnEnable = serializedObject.FindProperty("m_playOnEnable");
            m_useUnscaledTime = serializedObject.FindProperty("m_useUnscaledTime");
            m_speed = serializedObject.FindProperty("m_speed");
            m_stagger = serializedObject.FindProperty("m_stagger");
            m_hurrySpeed = serializedObject.FindProperty("m_hurrySpeed");
            m_completed = serializedObject.FindProperty("m_completed");
        }

        /// <summary>
        /// Keeps the playing time and highlight current while an animation runs in play mode.
        /// </summary>
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying && m_tweenline.IsPlaying;
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
            EditorGUILayout.PropertyField(m_hurrySpeed);

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

                    var playing = Application.isPlaying && m_tweenline.IsPlaying && m_tweenline.Current == animation;

                    if (Application.isPlaying) {
                        var reverseRect = new Rect(right - 24, rect.y, 24, rect.height);
                        var playRect = new Rect(reverseRect.x - 26, rect.y, 24, rect.height);
                        right = playRect.x - 2;

                        if (GUI.Button(playRect, new GUIContent("▶", $"Play {animation.Name}"), EditorStyles.miniButtonLeft)) {
                            m_tweenline.Play(animation, false);
                        }

                        if (GUI.Button(reverseRect, new GUIContent("◀", $"Play {animation.Name} in reverse"), EditorStyles.miniButtonRight)) {
                            m_tweenline.Play(animation, true);
                        }
                    }

                    var details = playing
                        ? $"{(m_tweenline.IsHurried ? "hurried" : "playing")}  {m_tweenline.Time:0.00}s / {animation.Duration:0.##}s"
                        : $"{animation.Tracks.Count} tracks  {animation.Duration:0.##}s  {animation.Wrap}";

                    var nameWidth = Mathf.Min(EditorGUIUtility.labelWidth, (right - rect.x) * 0.5f);
                    GUI.Label(new Rect(rect.x, rect.y, nameWidth, rect.height), animation.Name, playing ? EditorStyles.boldLabel : EditorStyles.label);
                    GUI.Label(new Rect(rect.x + nameWidth, rect.y, right - rect.x - nameWidth, rect.height), details, EditorStyles.miniLabel);
                }
            }

            if (Application.isPlaying) {
                DrawPlaybackButtons();
            }
        }

        private void DrawPlaybackButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(!m_tweenline.IsPlaying)) {
                if (GUILayout.Button(new GUIContent("Stop", "Stop where it is"), EditorStyles.miniButtonLeft)) {
                    m_tweenline.Stop();
                }

                using (new EditorGUI.DisabledScope(m_tweenline.IsHurried)) {
                    if (GUILayout.Button(new GUIContent("Hurry", "Speed up the current play by Hurry Speed, like a tap to skip"), EditorStyles.miniButtonMid)) {
                        m_tweenline.Hurry();
                    }
                }

                if (GUILayout.Button(new GUIContent("Complete", "Jump to the end and fire the remaining markers"), EditorStyles.miniButtonRight)) {
                    m_tweenline.Complete();
                }
            }
        }
    }
}
