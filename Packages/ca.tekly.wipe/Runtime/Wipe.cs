using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Wipes
{
    /// <summary>
    /// Covers its rect with a colour or a sprite through a transition: one quad in one pass.
    ///
    /// Coverage is how much is covered: 0 is none and 1 is all. Cover raises it and Uncover lowers it. The
    /// look decides whether Uncover carries on through or goes back the way it came.
    ///
    /// For a load screen, stretch a Wipe over a canvas above everything, or use the Screen Wipe, then
    /// await Cover, load, and await Uncover. For a billboard that turns from an advert into live UI, put a
    /// Wipe with the advert's sprite over the UI and uncover it. To have a full screen canvas itself appear
    /// through the transition over a live game, use a Wipe Reveal.
    ///
    /// Coverage changes only material values, so a tween never rebuilds the canvas mesh. At coverage 0 the
    /// wipe draws nothing and lets raycasts through. Above 0 it blocks them when Raycast Target is on. A
    /// wipe is not maskable: a stencil mask would draw from a copy of its material that changes never reach.
    /// </summary>
    [AddComponentMenu("UI/Tekly/Wipe")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class Wipe : MaskableGraphic, IWipe
    {
        private const int MAX_TRIM_PLANES = 2;

        public float Coverage {
            get => m_coverage;
            set => Set(value, m_leaving);
        }

        public bool Leaving {
            get => m_leaving;
            set => Set(m_coverage, value);
        }

        /// <summary>
        /// The shared look. Null uses the wipe's own style.
        /// </summary>
        public WipeLook Look {
            get => m_look;
            set {
                m_look = value;
                Refresh();
            }
        }

        /// <summary>
        /// The style being drawn: a track's or a Cover call's look, else the shared look, else the wipe's
        /// own. Call Refresh after changing it.
        /// </summary>
        public WipeStyle Style {
            get {
                if (m_drivenStyle != null) {
                    return m_drivenStyle;
                }

                return m_playLook != null ? m_playLook.Style : BaseStyle;
            }
        }

        /// <summary>
        /// Covers with this sprite instead of the look's fill colour, tinted by the graphic's colour.
        /// </summary>
        public Sprite Sprite {
            get => m_sprite;
            set {
                m_sprite = value;
                Refresh();
            }
        }

        public WipeFocus Focus {
            get => m_focus;
            set {
                m_focus = value;
                Refresh();
            }
        }

        /// <summary>
        /// Trims the quad to the covered part of a straight wipe, which saves fill rate but rebuilds the
        /// wipe's mesh as it moves. Only worth it when the wipe has a canvas to itself.
        /// </summary>
        public bool Trim {
            get => m_trim;
            set {
                m_trim = value;
                Refresh();
            }
        }

        /// <summary>
        /// False when the wipe draws nothing.
        /// </summary>
        public bool IsDrawing => m_drawing;

        /// <summary>
        /// True while a Cover or Uncover call is running.
        /// </summary>
        public bool IsPlaying => m_playback.IsPlaying;

        public override Texture mainTexture => s_WhiteTexture;
        public override Material defaultMaterial => m_material != null ? m_material : base.defaultMaterial;

        private WipeStyle BaseStyle => m_look != null ? m_look.Style : m_style;

        [Tooltip("A shared look. Empty uses the style below")]
        [SerializeField] private WipeLook m_look;

        [SerializeField] private WipeStyle m_style = new WipeStyle();

        [Tooltip("Covers with this sprite instead of the look's fill colour, tinted by Color")]
        [SerializeField] private Sprite m_sprite;

        [Tooltip("How much is covered: 0 is none and 1 is all")]
        [SerializeField, Range(0, 1)] private float m_coverage;

        [SerializeField, HideInInspector] private bool m_leaving;

        [Tooltip("Centers a Radial, Angular or Shape field on a target instead of the look's center")]
        [SerializeField] private WipeFocus m_focus;

        [Tooltip("Trims the quad to the covered part of a straight wipe. Saves fill rate but rebuilds the mesh as it moves, so only turn it on when the wipe has a canvas to itself")]
        [SerializeField] private bool m_trim;

        private static readonly List<Vector2> s_polygon = new List<Vector2>(8);
        private static readonly List<Vector2> s_clipped = new List<Vector2>(8);
        private static Shader s_shader;

        [NonSerialized] private Material m_material;
        [NonSerialized] private WipeLook m_playLook;
        [NonSerialized] private WipeStyle m_drivenStyle;
        [NonSerialized] private Texture m_drivenFill;
        [NonSerialized] private Color m_backdrop = Color.clear;

        [NonSerialized] private bool m_drawing;
        [NonSerialized] private bool m_meshDrawing;
        [NonSerialized] private bool m_meshTrimmed;
        [NonSerialized] private int m_trimCount;
        [NonSerialized] private readonly Vector3[] m_trimPlanes = new Vector3[MAX_TRIM_PLANES];

        [NonSerialized] private string m_fieldKeyword;
        [NonSerialized] private string m_repeatKeyword;
        [NonSerialized] private bool m_fillKeyword;

        [NonSerialized] private float m_appliedCoverage = -1f;
        [NonSerialized] private bool m_appliedLeaving;
        [NonSerialized] private bool m_followFocus;
        [NonSerialized] private bool m_watching;
        [NonSerialized] private readonly WipePlayback m_playback = new WipePlayback();

        /// <summary>
        /// Covers the rect over the look's cover duration. Completes when it is fully covered, which is the
        /// moment to swap what is behind. A look given here stays until the next call that gives one.
        /// </summary>
        public Awaitable Cover(WipeLook look = null)
        {
            return Play(look, true);
        }

        /// <summary>
        /// Uncovers the rect over the look's uncover duration. Completes when nothing is covered.
        /// </summary>
        public Awaitable Uncover(WipeLook look = null)
        {
            return Play(look, false);
        }

        /// <summary>
        /// Stops a running Cover or Uncover, leaving the coverage where it is.
        /// </summary>
        public void Stop()
        {
            m_playback.Stop();
        }

        /// <summary>
        /// Sets the coverage and whether the wipe is leaving in one go.
        /// </summary>
        public void Set(float coverage, bool leaving)
        {
            coverage = Mathf.Clamp01(coverage);

            if (m_coverage == coverage && m_leaving == leaving) {
                return;
            }

            m_coverage = coverage;
            m_leaving = leaving;
            Refresh();
        }

        public WipeStyle StyleFor(WipeLook look)
        {
            return look != null ? look.Style : BaseStyle;
        }

        public void SetTrackState(WipeLook look, bool moving)
        {
            if (m_playLook != look) {
                m_playLook = look;
                Refresh();
            }
        }

        /// <summary>
        /// Pushes the style and coverage to the material. Call it after changing the style from code.
        /// </summary>
        public void Refresh()
        {
            if (m_material == null) {
                return;
            }

            var style = Style;
            var frame = BuildFrame(style);

            ApplyKeywords(style, frame.FillTexture != null);
            WipeDraw.Apply(m_material, style, frame);

            m_drawing = m_coverage > 0f;
            m_trimCount = m_trim && m_drawing ? WipeDraw.Trim(style, frame, m_trimPlanes) : 0;
            m_appliedCoverage = m_coverage;
            m_appliedLeaving = m_leaving;

            // Material values need no rebuild. The mesh does when it is or was trimmed, or turns on or off.
            if (m_drawing != m_meshDrawing || m_trimCount > 0 || m_meshTrimmed) {
                SetVerticesDirty();
            }

            // A focus on something that moves is followed while the wipe is part way.
            m_followFocus = m_focus != null && m_focus.Moves && m_drawing && m_coverage < 1f && WipeStyle.HasCenter(style.EffectiveField);
            UpdateWatching();
        }

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            return m_drawing && base.Raycast(screenPoint, eventCamera);
        }

        /// <summary>
        /// Draws with a style and fill owned by something else, such as a Wipe Reveal's content.
        /// </summary>
        internal void Drive(WipeStyle style, Texture fill, Color backdrop)
        {
            m_drivenStyle = style;
            m_drivenFill = fill;
            m_backdrop = backdrop;
            Refresh();
        }

        internal static void DestroySafely(UnityEngine.Object target)
        {
            if (Application.isPlaying) {
                Destroy(target);
            } else {
                DestroyImmediate(target);
            }
        }

        protected override void OnEnable()
        {
            if (m_material == null) {
                if (s_shader == null) {
                    s_shader = Resources.Load<Shader>(WipeDraw.SHADER_RESOURCE);
                }

                if (s_shader != null) {
                    m_material = new Material(s_shader) {
                        name = "Wipe",
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    m_fieldKeyword = null;
                    m_repeatKeyword = null;
                    m_fillKeyword = false;
                } else {
                    Debug.LogError($"[{name}] can't find the wipe shader [{WipeDraw.SHADER_RESOURCE}]", this);
                }
            }

            base.OnEnable();
            Refresh();
        }

        protected override void OnDisable()
        {
            Stop();
            m_followFocus = false;
            UpdateWatching();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (m_material != null) {
                DestroySafely(m_material);
                m_material = null;
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Refresh();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            Refresh();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            m_meshDrawing = m_drawing;
            m_meshTrimmed = m_trimCount > 0;

            if (!m_drawing) {
                return;
            }

            var rect = rectTransform.rect;
            var height = Mathf.Max(rect.height, 1e-3f);
            var half = new Vector2(rect.width / height * 0.5f, 0.5f);

            s_polygon.Clear();
            s_polygon.Add(new Vector2(-half.x, -half.y));
            s_polygon.Add(new Vector2(-half.x, half.y));
            s_polygon.Add(new Vector2(half.x, half.y));
            s_polygon.Add(new Vector2(half.x, -half.y));

            for (var i = 0; i < m_trimCount && s_polygon.Count > 2; i++) {
                ClipPolygon(m_trimPlanes[i]);
            }

            if (s_polygon.Count < 3) {
                return;
            }

            var center = rect.center;

            // The colour comes from the material. The vertex alpha is left for a CanvasGroup's fade.
            Color32 white = Color.white;

            foreach (var point in s_polygon) {
                var position = new Vector3(center.x + point.x * height, center.y + point.y * height);
                vh.AddVert(position, white, new Vector4(point.x, point.y, 0, 0));
            }

            for (var i = 1; i < s_polygon.Count - 1; i++) {
                vh.AddTriangle(0, i, i + 1);
            }
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            maskable = false;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            Refresh();
        }
#endif

        private Awaitable Play(WipeLook look, bool cover)
        {
            if (look != null) {
                m_playLook = look;
                Refresh();
            }

            return m_playback.Play(this, Style, cover, destroyCancellationToken);
        }

        private WipeFrame BuildFrame(WipeStyle style)
        {
            var rect = rectTransform.rect;

            var frame = new WipeFrame {
                Aspect = Mathf.Max(rect.width, 1e-3f) / Mathf.Max(rect.height, 1e-3f),
                Coverage = m_coverage,
                Leaving = m_leaving,
                Backdrop = m_backdrop,
                FillRect = new Vector4(0, 0, 1, 1)
            };

            if (m_drivenFill != null) {
                // A canvas render is already premultiplied.
                frame.FillTexture = m_drivenFill;
                frame.FillPremultiplied = true;
                frame.Fill = color;
            } else if (m_drivenStyle != null) {
                // Driven with nothing to show yet: the fill is a hole.
                frame.Fill = Color.clear;
            } else if (m_sprite != null) {
                var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(m_sprite);

                frame.FillTexture = m_sprite.texture;
                frame.FillRect = new Vector4(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y);
                frame.Fill = color;
            } else {
                frame.Fill = style.Fill * color;
            }

            if (m_focus != null && WipeStyle.HasCenter(style.EffectiveField) && m_focus.TryGetScreenPoint(out var screenPoint)) {
                var root = canvas;
                var eventCamera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local)) {
                    frame.HasFocus = true;
                    frame.Focus = new Vector2(
                        (local.x - rect.xMin) / Mathf.Max(rect.width, 1e-3f),
                        (local.y - rect.yMin) / Mathf.Max(rect.height, 1e-3f));
                }
            }

            return frame;
        }

        private void ApplyKeywords(WipeStyle style, bool fillTexture)
        {
            var field = WipeDraw.FieldKeyword(style.EffectiveField);

            if (field != m_fieldKeyword) {
                SwapKeyword(m_fieldKeyword, field);
                m_fieldKeyword = field;
            }

            var repeat = WipeDraw.RepeatKeyword(style.EffectiveRepeat);

            if (repeat != m_repeatKeyword) {
                SwapKeyword(m_repeatKeyword, repeat);
                m_repeatKeyword = repeat;
            }

            if (fillTexture != m_fillKeyword) {
                m_fillKeyword = fillTexture;
                SwapKeyword(fillTexture ? null : WipeDraw.KEYWORD_FILL_TEXTURE, fillTexture ? WipeDraw.KEYWORD_FILL_TEXTURE : null);
            }
        }

        private void SwapKeyword(string previous, string next)
        {
            if (previous != null) {
                m_material.DisableKeyword(previous);
            }

            if (next != null) {
                m_material.EnableKeyword(next);
            }
        }

        /// <summary>
        /// Watches the canvas render while there is something to pick up that no setter reports: a focus
        /// that moves, or in edit mode a previewed value that was put back behind the wipe's back.
        /// </summary>
        private void UpdateWatching()
        {
            var watch = m_followFocus;

#if UNITY_EDITOR
            watch |= !Application.isPlaying && isActiveAndEnabled;
#endif

            if (watch == m_watching) {
                return;
            }

            m_watching = watch;

            if (watch) {
                Canvas.willRenderCanvases += OnWillRenderCanvases;
            } else {
                Canvas.willRenderCanvases -= OnWillRenderCanvases;
            }
        }

        private void OnWillRenderCanvases()
        {
            if (this == null) {
                Canvas.willRenderCanvases -= OnWillRenderCanvases;
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying) {
                // Stopping a Tweenline preview restores the serialized values without calling a setter,
                // and the clip's look is no longer wanted.
                var previewEnded = m_playLook != null && !UnityEditor.AnimationMode.InAnimationMode();

                if (previewEnded) {
                    m_playLook = null;
                }

                if (previewEnded || m_coverage != m_appliedCoverage || m_leaving != m_appliedLeaving) {
                    Refresh();
                }

                return;
            }
#endif

            if (m_followFocus) {
                Refresh();
            }
        }

        /// <summary>
        /// Keeps the part of the polygon where dot(point, normal) is at most the plane's limit.
        /// </summary>
        private static void ClipPolygon(Vector3 plane)
        {
            var normal = new Vector2(plane.x, plane.y);
            var count = s_polygon.Count;

            s_clipped.Clear();

            for (var i = 0; i < count; i++) {
                var a = s_polygon[i];
                var b = s_polygon[(i + 1) % count];
                var distanceA = Vector2.Dot(a, normal) - plane.z;
                var distanceB = Vector2.Dot(b, normal) - plane.z;

                if (distanceA <= 0) {
                    s_clipped.Add(a);
                }

                if ((distanceA < 0 && distanceB > 0) || (distanceA > 0 && distanceB < 0)) {
                    s_clipped.Add(a + (b - a) * (distanceA / (distanceA - distanceB)));
                }
            }

            s_polygon.Clear();
            s_polygon.AddRange(s_clipped);
        }
    }
}
