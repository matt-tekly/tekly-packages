using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tekly.Wipes
{
    /// <summary>
    /// A preview of a look in the inspector, drawn with the Wipe shader so it matches what a Wipe draws.
    /// It can be scrubbed, switched between covering and uncovering, and played with the look's own
    /// durations and eases. One belongs to each inspector that shows a style.
    /// </summary>
    public sealed class WipePreview : IDisposable
    {
        private const float MAX_HEIGHT = 240f;

        // Seconds the played preview waits fully covered and fully uncovered.
        private const float HOLD = 0.4f;

        private static readonly string[] s_modeNames = { "Cover", "Uncover" };
        private static readonly string[] s_aspectNames = { "16:9", "4:3", "9:16" };
        private static readonly float[] s_aspects = { 16f / 9f, 4f / 3f, 9f / 16f };

        // Stands in for whatever the wipe covers.
        private static readonly Color s_backdrop = new Color(0.82f, 0.86f, 0.90f);

        private static readonly Vector3[] s_vertices = new Vector3[4];
        private static readonly List<Vector4> s_uvs = new List<Vector4>(4);
        private static readonly List<string> s_keywords = new List<string>(3);

        private readonly Editor m_owner;

        private Material m_material;
        private Mesh m_mesh;
        private float m_coverage = 0.5f;
        private bool m_uncover;
        private int m_aspect;
        private bool m_playing;
        private double m_lastTime;
        private float m_time;

        public WipePreview(Editor owner)
        {
            m_owner = owner;
        }

        public void Dispose()
        {
            Stop();

            if (m_material != null) {
                UnityEngine.Object.DestroyImmediate(m_material);
                m_material = null;
            }

            if (m_mesh != null) {
                UnityEngine.Object.DestroyImmediate(m_mesh);
                m_mesh = null;
            }
        }

        public void Draw(WipeStyle style)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (m_playing) {
                Advance(style);
            }

            using (new EditorGUILayout.HorizontalScope()) {
                var playLabel = new GUIContent(m_playing ? "Stop" : "Play", "Plays with the look's Timing From Code. A Tweenline clip uses its own duration and ease instead");

                if (GUILayout.Button(playLabel, GUILayout.Width(56))) {
                    if (m_playing) {
                        Stop();
                    } else {
                        Play();
                    }
                }

                EditorGUI.BeginChangeCheck();
                var mode = GUILayout.Toolbar(m_uncover ? 1 : 0, s_modeNames, GUILayout.Width(140));

                if (EditorGUI.EndChangeCheck()) {
                    Stop();
                    m_uncover = mode == 1;
                }

                GUILayout.FlexibleSpace();
                m_aspect = EditorGUILayout.Popup(m_aspect, s_aspectNames, GUILayout.Width(56));
            }

            EditorGUI.BeginChangeCheck();
            var coverage = EditorGUILayout.Slider(new GUIContent("Coverage", "How much is covered. In Uncover the look's Exit decides whether it carries on through or goes back"), m_coverage, 0f, 1f);

            if (EditorGUI.EndChangeCheck()) {
                Stop();
                m_coverage = coverage;
            }

            var aspect = s_aspects[m_aspect];
            var height = Mathf.Min((EditorGUIUtility.currentViewWidth - 40f) / aspect, MAX_HEIGHT);
            var width = height * aspect;

            using (new EditorGUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
                GUILayout.FlexibleSpace();

                if (Event.current.type == EventType.Repaint) {
                    var scale = EditorGUIUtility.pixelsPerPoint;
                    var target = RenderTexture.GetTemporary(Mathf.Max(1, Mathf.RoundToInt(width * scale)), Mathf.Max(1, Mathf.RoundToInt(height * scale)), 0, RenderTextureFormat.ARGB32);

                    Render(target, style, m_coverage, m_uncover);
                    GUI.DrawTexture(rect, target, ScaleMode.StretchToFill, false);

                    RenderTexture.ReleaseTemporary(target);
                }
            }
        }

        /// <summary>
        /// Draws a style into a texture at a coverage, over a plain backdrop. Uncover with a look that
        /// carries on through draws the leaving half.
        /// </summary>
        public void Render(RenderTexture target, WipeStyle style, float coverage, bool uncover)
        {
            if (m_material == null) {
                var shader = Resources.Load<Shader>(WipeDraw.SHADER_RESOURCE);

                if (shader == null) {
                    return;
                }

                m_material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                m_mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            }

            var aspect = (float) target.width / target.height;
            var half = new Vector2(aspect * 0.5f, 0.5f);

            var frame = new WipeFrame {
                Aspect = aspect,
                Coverage = coverage,
                Leaving = uncover && style.ExitContinues,
                Fill = style.Fill,
                FillRect = new Vector4(0, 0, 1, 1),
                Backdrop = Color.clear
            };

            s_keywords.Clear();
            AddKeyword(WipeDraw.FieldKeyword(style.EffectiveField));
            AddKeyword(WipeDraw.RepeatKeyword(style.EffectiveRepeat));

            m_material.shaderKeywords = s_keywords.ToArray();
            WipeDraw.Apply(m_material, style, frame);

            // The shader reads a position from the rect's center in rect heights from the first uv
            s_uvs.Clear();

            for (var i = 0; i < 4; i++) {
                var corner = new Vector2((i & 1) == 0 ? -half.x : half.x, (i & 2) == 0 ? -half.y : half.y);
                s_vertices[i] = corner;
                s_uvs.Add(new Vector4(corner.x, corner.y, 0, 0));
            }

            m_mesh.Clear();
            m_mesh.vertices = s_vertices;
            m_mesh.SetUVs(0, s_uvs);
            m_mesh.colors32 = new Color32[] { Color.white, Color.white, Color.white, Color.white };
            m_mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };

            var previous = RenderTexture.active;
            RenderTexture.active = target;

            var backdrop = QualitySettings.activeColorSpace == ColorSpace.Linear ? s_backdrop.linear : s_backdrop;
            GL.Clear(true, true, backdrop);

            GL.PushMatrix();
            GL.LoadIdentity();
            GL.LoadProjectionMatrix(Matrix4x4.Ortho(-half.x, half.x, -half.y, half.y, -1f, 1f));

            if (m_material.SetPass(0)) {
                Graphics.DrawMeshNow(m_mesh, Matrix4x4.identity);
            }

            GL.PopMatrix();
            RenderTexture.active = previous;
        }

        private static void AddKeyword(string keyword)
        {
            if (keyword != null) {
                s_keywords.Add(keyword);
            }
        }

        private void Play()
        {
            m_playing = true;
            m_time = 0f;
            m_lastTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private void Stop()
        {
            if (m_playing) {
                m_playing = false;
                EditorApplication.update -= Tick;
            }
        }

        private void Tick()
        {
            if (m_owner == null) {
                Stop();
                return;
            }

            m_owner.Repaint();
        }

        /// <summary>
        /// Plays a cover, a hold, an uncover and a hold in a loop, with the look's durations and eases.
        /// </summary>
        private void Advance(WipeStyle style)
        {
            var now = EditorApplication.timeSinceStartup;
            m_time += (float) (now - m_lastTime);
            m_lastTime = now;

            var cover = Mathf.Max(style.CoverDuration, 0.01f);
            var uncover = Mathf.Max(style.UncoverDuration, 0.01f);
            var time = m_time % (cover + HOLD + uncover + HOLD);

            if (time < cover) {
                m_uncover = false;
                m_coverage = Mathf.Clamp01(style.CoverEase.Evaluate(time / cover));
            } else if (time < cover + HOLD) {
                m_uncover = false;
                m_coverage = 1f;
            } else if (time < cover + HOLD + uncover) {
                m_uncover = true;
                m_coverage = Mathf.Clamp01(1f - style.UncoverEase.Evaluate((time - cover - HOLD) / uncover));
            } else {
                m_uncover = true;
                m_coverage = 0f;
            }
        }
    }
}
