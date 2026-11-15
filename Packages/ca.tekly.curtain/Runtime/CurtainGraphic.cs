using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Curtains
{
    /// <summary>
    /// Draws a curtain: one quad in one pass, where each pixel is covered or not by comparing the style's
    /// field with the progress, so whatever is behind shows through the open parts.
    ///
    /// Up to four bands follow each other with a stagger, all in the same pass. A later band replaces the
    /// ones before it, so a clear band wipes a hole. A curtain with content draws it as its last band.
    ///
    /// Progress changes only material values, so a tween never rebuilds the canvas mesh, except that the
    /// quad is trimmed to the covered part for wipes, splits and irises. Give the curtain its own canvas so
    /// those small rebuilds don't touch the rest of the UI. A curtain is not maskable: a stencil mask would
    /// draw from a copy of its material that progress changes never reach.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class CurtainGraphic : MaskableGraphic
    {
        public const int MAX_BANDS = 4;

        private const string SHADER_RESOURCE = "Shaders/Curtain";
        private const string KEYWORD_IRIS = "CURTAIN_IRIS";
        private const string KEYWORD_GRADIENT = "CURTAIN_GRADIENT";
        private const string KEYWORD_CONTENT = "CURTAIN_CONTENT";

        // The slot the content band always uses, so it draws on top.
        private const int CONTENT_SLOT = MAX_BANDS - 1;

        // Progress for an unused band, far enough below 0 that it never covers anything.
        private const float UNUSED_BAND = -16f;

        // Room the trimmed quad leaves for the antialiased edge, as a fraction of the field.
        private const float TRIM_MARGIN = 0.02f;

        private static readonly int s_contentId = Shader.PropertyToID("_Content");
        private static readonly int s_gradientId = Shader.PropertyToID("_Gradient");
        private static readonly int s_progressId = Shader.PropertyToID("_Progress");
        private static readonly int s_fieldId = Shader.PropertyToID("_Field");
        private static readonly int s_shapeId = Shader.PropertyToID("_Shape");
        private static readonly int s_gradientRangeId = Shader.PropertyToID("_GradientRange");
        private static readonly int s_softnessId = Shader.PropertyToID("_Softness");

        private static readonly int[] s_colorIds = {
            Shader.PropertyToID("_Color0"),
            Shader.PropertyToID("_Color1"),
            Shader.PropertyToID("_Color2"),
            Shader.PropertyToID("_Color3")
        };

        private static readonly List<Vector2> s_polygon = new List<Vector2>(8);
        private static readonly List<Vector2> s_clipped = new List<Vector2>(8);

        /// <summary>
        /// False when the curtain draws nothing. It skips its draw and lets raycasts through then.
        /// </summary>
        public bool IsDrawing => m_drawing;

        public override Texture mainTexture => s_WhiteTexture;
        public override Material defaultMaterial => m_material != null ? m_material : base.defaultMaterial;

        protected abstract CurtainStyle DrawStyle { get; }
        protected abstract float DrawProgress { get; }

        /// <summary>
        /// True when the curtain ends with a content band after its colour bands. Colour bands are then
        /// limited to three.
        /// </summary>
        protected virtual bool HasContentBand => false;

        /// <summary>
        /// What the content band shows. Null draws it clear.
        /// </summary>
        protected virtual Texture ContentTexture => null;

        /// <summary>
        /// True when the curtain should draw nothing, whatever its progress.
        /// </summary>
        protected virtual bool SkipDraw => false;

        [NonSerialized] private Material m_material;
        [NonSerialized] private bool m_drawing;
        [NonSerialized] private bool m_meshDrawing;
        [NonSerialized] private bool m_meshTrimmed;
        [NonSerialized] private bool m_trimmed;
        [NonSerialized] private int m_keywordShape = -1;
        [NonSerialized] private int m_keywordContent = -1;

        // The planes the quad is trimmed to, in rect heights from the center: xy = normal, z = limit.
        [NonSerialized] private readonly Vector3[] m_planes = new Vector3[4];
        [NonSerialized] private int m_planeCount;

        /// <summary>
        /// Pushes the style and progress to the material. Call it after changing the style.
        /// </summary>
        public void Refresh()
        {
            if (m_material == null) {
                return;
            }

            ApplyKeywords();
            ApplyProperties();

            // Material values need no rebuild. The mesh does when it is or was trimmed, or turns on or off.
            if (m_trimmed || m_meshTrimmed || m_drawing != m_meshDrawing) {
                SetVerticesDirty();
            }
        }

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            return m_drawing && base.Raycast(screenPoint, eventCamera);
        }

        protected override void OnEnable()
        {
            if (m_material == null) {
                var shader = Resources.Load<Shader>(SHADER_RESOURCE);

                if (shader != null) {
                    m_material = new Material(shader) {
                        name = "Curtain",
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    m_keywordShape = -1;
                    m_keywordContent = -1;
                } else {
                    Debug.LogError($"[{name}] can't find the curtain shader [{SHADER_RESOURCE}]", this);
                }
            }

            base.OnEnable();
            Refresh();
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

            ApplyProperties();
            m_meshDrawing = m_drawing;
            m_meshTrimmed = m_trimmed;

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

            for (var i = 0; i < m_planeCount && s_polygon.Count > 2; i++) {
                ClipPolygon(m_planes[i]);
            }

            if (s_polygon.Count < 3) {
                return;
            }

            var center = rect.center;
            Color32 vertexColor = color;

            foreach (var point in s_polygon) {
                var position = new Vector3(center.x + point.x * height, center.y + point.y * height);
                vh.AddVert(position, vertexColor, new Vector4(point.x, point.y, 0, 0));
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

        internal static void DestroySafely(UnityEngine.Object target)
        {
            if (Application.isPlaying) {
                Destroy(target);
            } else {
                DestroyImmediate(target);
            }
        }

        /// <summary>
        /// The shape the shader draws. A Gradient with no texture falls back to a Diagonal.
        /// </summary>
        private static CurtainShape EffectiveShape(CurtainStyle style)
        {
            if (style.Shape == CurtainShape.Gradient && (style.Gradient == null || style.Gradient.Texture == null)) {
                return CurtainShape.Diagonal;
            }

            return style.Shape;
        }

        private void ApplyKeywords()
        {
            var style = DrawStyle;
            var shape = style != null ? (int) EffectiveShape(style) : 0;

            if (shape != m_keywordShape) {
                m_keywordShape = shape;
                SetKeyword(KEYWORD_IRIS, shape == (int) CurtainShape.Iris);
                SetKeyword(KEYWORD_GRADIENT, shape == (int) CurtainShape.Gradient);
            }

            var content = HasContentBand && ContentTexture != null ? 1 : 0;

            if (content != m_keywordContent) {
                m_keywordContent = content;
                SetKeyword(KEYWORD_CONTENT, content == 1);
            }
        }

        private void SetKeyword(string keyword, bool enabled)
        {
            if (enabled) {
                m_material.EnableKeyword(keyword);
            } else {
                m_material.DisableKeyword(keyword);
            }
        }

        private void ApplyProperties()
        {
            var style = DrawStyle;
            m_planeCount = 0;
            m_trimmed = false;

            if (m_material == null || style == null) {
                m_drawing = false;
                return;
            }

            // The content band comes after the colour bands and always uses the top slot.
            var hasContent = HasContentBand;
            var colorCount = Mathf.Min(style.Bands.Count, hasContent ? MAX_BANDS - 1 : MAX_BANDS);
            var count = colorCount + (hasContent ? 1 : 0);

            var stagger = count > 1 ? Mathf.Min(style.Stagger, 0.95f / (count - 1)) : 0f;
            var span = 1f - stagger * (count - 1);

            // Past 1 the curtain is leaving: the same sweep again, with the bands in reverse and uncovering.
            var drawProgress = DrawProgress;
            var leaving = drawProgress > CurtainProgress.COVERED;
            var phase = leaving ? drawProgress - CurtainProgress.COVERED : drawProgress;

            var progress = new Vector4(UNUSED_BAND, UNUSED_BAND, UNUSED_BAND, UNUSED_BAND);
            var most = UNUSED_BAND;

            for (var i = 0; i < MAX_BANDS; i++) {
                m_material.SetColor(s_colorIds[i], Color.clear);
            }

            for (var i = 0; i < count; i++) {
                // Leaving, the last band goes first. The field is flipped below so it carries on the way
                // it came in.
                var order = leaving ? count - 1 - i : i;
                var travel = style.Ease.Evaluate(Mathf.Clamp01((phase - order * stagger) / span));
                var band = leaving ? 1f - travel : travel;

                var isContent = i == colorCount;
                var slot = isContent ? CONTENT_SLOT : i;

                progress[slot] = band;
                most = Mathf.Max(most, band);

                if (!isContent) {
                    m_material.SetColor(s_colorIds[slot], style.Bands[i]);
                }
            }

            var flip = style.Invert != leaving;

            m_material.SetVector(s_progressId, progress);
            m_material.SetVector(s_fieldId, new Vector4(flip ? -1f : 1f, flip ? 1f : 0f, 0, 0));
            m_material.SetFloat(s_softnessId, style.Softness);
            m_material.SetTexture(s_contentId, hasContent ? ContentTexture : null);

            var rect = rectTransform.rect;
            var height = Mathf.Max(rect.height, 1e-3f);
            var aspect = rect.width / height;
            var half = new Vector2(aspect * 0.5f, 0.5f);

            m_drawing = most > 0f && !SkipDraw;

            // Past the edge, antialiasing and softness included, the curtain draws nothing.
            var limit = most * (1f + style.Softness + TRIM_MARGIN);

            switch (EffectiveShape(style)) {
                case CurtainShape.Diagonal: {
                    var radians = style.Angle * Mathf.Deg2Rad;
                    var direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
                    var extent = Mathf.Abs(direction.x) * half.x + Mathf.Abs(direction.y) * half.y;
                    var scaled = direction / Mathf.Max(extent, 1e-4f);

                    m_material.SetVector(s_shapeId, new Vector4(scaled.x, scaled.y, style.Split ? 1f : 0f, 0));

                    if (style.Split) {
                        // Opening from the center line is a slab. From the edges in it is two pieces.
                        if (!flip) {
                            AddPlane(scaled, limit);
                            AddPlane(-scaled, limit);
                        }
                    } else {
                        AddPlane(flip ? -scaled : scaled, 2f * limit - 1f);
                    }

                    break;
                }
                case CurtainShape.Iris: {
                    var center = (style.Center - new Vector2(0.5f, 0.5f)) * new Vector2(aspect, 1f);
                    var farthest = 1e-4f;

                    for (var i = 0; i < 4; i++) {
                        var corner = new Vector2((i & 1) == 0 ? -half.x : half.x, (i & 2) == 0 ? -half.y : half.y);
                        var offset = corner - center;
                        var distance = style.Diamond ? Mathf.Abs(offset.x) + Mathf.Abs(offset.y) : offset.magnitude;
                        farthest = Mathf.Max(farthest, distance);
                    }

                    m_material.SetVector(s_shapeId, new Vector4(center.x, center.y, 1f / farthest, style.Diamond ? 1f : 0f));

                    // Growing from the center is a box around the circle. Closing in from outside covers
                    // everything but a hole.
                    if (!flip) {
                        var radius = limit * farthest;
                        AddPlane(new Vector2(1, 0), center.x + radius);
                        AddPlane(new Vector2(-1, 0), radius - center.x);
                        AddPlane(new Vector2(0, 1), center.y + radius);
                        AddPlane(new Vector2(0, -1), radius - center.y);
                    }

                    break;
                }
                case CurtainShape.Gradient: {
                    var texture = style.Gradient.Texture;
                    var textureAspect = (float) texture.width / Mathf.Max(texture.height, 1);

                    // Cover: the ramp keeps its aspect and fills the rect, cropping the overflow.
                    var visible = aspect >= textureAspect
                        ? new Vector2(1f, textureAspect / aspect)
                        : new Vector2(aspect / textureAspect, 1f);

                    var uvRect = new Rect(0.5f - visible.x * 0.5f, 0.5f - visible.y * 0.5f, visible.x, visible.y);
                    style.Gradient.GetRange(uvRect, out var least, out var greatest);

                    // Stretches the progress over the values left after the crop, so there is no dead time.
                    var range = Mathf.Max(greatest - least, 1f / 255f);

                    m_material.SetTexture(s_gradientId, texture);
                    m_material.SetVector(s_gradientRangeId, new Vector4(visible.x / aspect, visible.y, least, 1f / range));
                    break;
                }
            }

            m_trimmed = m_planeCount > 0;
        }

        private void AddPlane(Vector2 normal, float limit)
        {
            m_planes[m_planeCount++] = new Vector3(normal.x, normal.y, limit);
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
