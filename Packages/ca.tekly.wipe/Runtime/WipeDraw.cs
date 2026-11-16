using UnityEngine;

namespace Tekly.Wipes
{
    /// <summary>
    /// What a wipe needs to know about the rect it covers this refresh.
    /// </summary>
    internal struct WipeFrame
    {
        /// <summary>Width over height</summary>
        public float Aspect;

        public float Coverage;
        public bool Leaving;

        /// <summary>Replaces the style's center, from (0, 0) at the bottom left to (1, 1) at the top right</summary>
        public bool HasFocus;
        public Vector2 Focus;

        /// <summary>The fill colour, or the tint when there is a fill texture</summary>
        public Color Fill;
        public Texture FillTexture;

        /// <summary>The fill texture's uv offset in xy and scale in zw</summary>
        public Vector4 FillRect;
        public bool FillPremultiplied;

        public Color Backdrop;
    }

    /// <summary>
    /// Turns a style and a frame into the Wipe shader's keywords and values.
    /// </summary>
    internal static class WipeDraw
    {
        public const string SHADER_RESOURCE = "Tekly/Wipe";
        public const string KEYWORD_FILL_TEXTURE = "WIPE_FILL_TEXTURE";

        // A head for an unused layer, far enough below 0 that it never covers anything.
        private const float UNUSED = -16f;

        // Room a trimmed quad leaves for the antialiased edge, as a fraction of the field.
        private const float TRIM_MARGIN = 0.02f;

        private static readonly int s_fillId = Shader.PropertyToID("_Fill");
        private static readonly int s_rampId = Shader.PropertyToID("_Ramp");
        private static readonly int s_shapeId = Shader.PropertyToID("_ShapeTex");
        private static readonly int s_colorFillId = Shader.PropertyToID("_ColorFill");
        private static readonly int s_backdropId = Shader.PropertyToID("_Backdrop");
        private static readonly int s_headsId = Shader.PropertyToID("_Heads");
        private static readonly int s_tailsId = Shader.PropertyToID("_Tails");
        private static readonly int s_fieldAId = Shader.PropertyToID("_FieldA");
        private static readonly int s_fieldBId = Shader.PropertyToID("_FieldB");
        private static readonly int s_fieldOpId = Shader.PropertyToID("_FieldOp");
        private static readonly int s_waveId = Shader.PropertyToID("_Wave");
        private static readonly int s_repeatAId = Shader.PropertyToID("_RepeatA");
        private static readonly int s_repeatBId = Shader.PropertyToID("_RepeatB");
        private static readonly int s_repeatCId = Shader.PropertyToID("_RepeatC");
        private static readonly int s_fillRectId = Shader.PropertyToID("_FillRect");
        private static readonly int s_fillPremultipliedId = Shader.PropertyToID("_FillPremultiplied");
        private static readonly int s_rectScaleId = Shader.PropertyToID("_RectScale");

        private static readonly int[] s_colorIds = {
            Shader.PropertyToID("_Color0"),
            Shader.PropertyToID("_Color1"),
            Shader.PropertyToID("_Color2")
        };

        /// <summary>
        /// The shader keyword for a field, or null for Linear.
        /// </summary>
        public static string FieldKeyword(WipeField field)
        {
            switch (field) {
                case WipeField.Radial:
                    return "WIPE_RADIAL";
                case WipeField.Angular:
                    return "WIPE_ANGULAR";
                case WipeField.Ramp:
                    return "WIPE_RAMP";
                case WipeField.Shape:
                    return "WIPE_SHAPE";
                case WipeField.Fade:
                    return "WIPE_FADE";
                default:
                    return null;
            }
        }

        /// <summary>
        /// The shader keyword for a repeat, or null for none.
        /// </summary>
        public static string RepeatKeyword(WipeRepeat repeat)
        {
            switch (repeat) {
                case WipeRepeat.Lanes:
                    return "WIPE_LANES";
                case WipeRepeat.Grid:
                    return "WIPE_GRID";
                default:
                    return null;
            }
        }

        public static void Apply(Material material, WipeStyle style, in WipeFrame frame)
        {
            var field = style.EffectiveField;
            var half = new Vector2(frame.Aspect * 0.5f, 0.5f);

            Layers(style, frame, out var heads, out var tails, out _, out _);

            material.SetVector(s_headsId, heads);
            material.SetVector(s_tailsId, tails);

            var layerCount = LayerCount(style);

            for (var i = 0; i < WipeStyle.MAX_LAYERS; i++) {
                material.SetColor(s_colorIds[i], i < layerCount ? style.Layers[i] : Color.clear);
            }

            material.SetColor(s_colorFillId, frame.Fill);
            material.SetColor(s_backdropId, frame.Backdrop);

            var invert = style.Invert;
            material.SetVector(s_fieldOpId, new Vector4(invert ? -1f : 1f, invert ? 1f : 0f, style.Softness, invert ? 1f : 0f));

            ApplyField(material, style, field, frame, half);
            ApplyRepeat(material, style, half);

            material.SetTexture(s_fillId, frame.FillTexture);
            material.SetVector(s_fillRectId, frame.FillRect);
            material.SetFloat(s_fillPremultipliedId, frame.FillPremultiplied ? 1f : 0f);
            material.SetVector(s_rectScaleId, new Vector4(1f / frame.Aspect, 1f, 0, 0));
        }

        /// <summary>
        /// Fills the planes a wipe's quad can be trimmed to, as a normal in xy and a limit in z: the part
        /// where dot(position, normal) is at most the limit is kept. Only a plain straight wipe is bounded
        /// by planes. Returns how many there are.
        /// </summary>
        public static int Trim(WipeStyle style, in WipeFrame frame, Vector3[] planes)
        {
            if (style.EffectiveField != WipeField.Linear || style.Mirror || style.EffectiveRepeat != WipeRepeat.None || style.WaveAmplitude > 0f) {
                return 0;
            }

            Layers(style, frame, out _, out _, out var least, out var most);

            var half = new Vector2(frame.Aspect * 0.5f, 0.5f);
            var scaled = ScaledDirection(half, style.Angle);
            var margin = style.Softness + TRIM_MARGIN;

            // The covered part runs from the slowest tail to the fastest head, edges included.
            var low = least - margin;
            var high = most + margin;

            if (style.Invert) {
                (low, high) = (1f - high, 1f - low);
            }

            var count = 0;

            // The field is the position along the direction, from -1 to 1, brought to 0 to 1.
            if (high < 1f) {
                planes[count++] = new Vector3(scaled.x, scaled.y, 2f * high - 1f);
            }

            if (low > 0f) {
                planes[count++] = new Vector3(-scaled.x, -scaled.y, 1f - 2f * low);
            }

            return count;
        }

        private static int LayerCount(WipeStyle style)
        {
            return style.EffectiveField == WipeField.Fade ? 0 : Mathf.Min(style.Layers.Count, WipeStyle.MAX_LAYERS);
        }

        /// <summary>
        /// Every layer's head and tail, the fill in w. Covering, the layers arrive in order and the fill
        /// ends on top. Leaving, the fill's tail goes first and the layers follow it out, so the wipe
        /// carries on the way it came in. least and most bound the covered part of the field.
        /// </summary>
        private static void Layers(WipeStyle style, in WipeFrame frame, out Vector4 heads, out Vector4 tails, out float least, out float most)
        {
            var layerCount = LayerCount(style);
            var count = layerCount + 1;
            var leaving = frame.Leaving && style.ExitContinues;

            var offset = count > 1 ? Mathf.Min(style.LayerOffset, 0.95f / (count - 1)) : 0f;
            var span = 1f - offset * (count - 1);

            heads = new Vector4(UNUSED, UNUSED, UNUSED, UNUSED);
            tails = Vector4.zero;
            least = 1f;
            most = 0f;

            for (var i = 0; i < count; i++) {
                var slot = i == layerCount ? 3 : i;

                if (leaving) {
                    var uncovered = Mathf.Clamp01((1f - frame.Coverage - (count - 1 - i) * offset) / span);
                    heads[slot] = 1f;
                    tails[slot] = uncovered;
                    least = Mathf.Min(least, uncovered);
                    most = 1f;
                } else {
                    var covered = Mathf.Clamp01((frame.Coverage - i * offset) / span);
                    heads[slot] = covered;
                    least = 0f;
                    most = Mathf.Max(most, covered);
                }
            }
        }

        private static void ApplyField(Material material, WipeStyle style, WipeField field, in WipeFrame frame, Vector2 half)
        {
            var center = FromNormalized(frame.Aspect, frame.HasFocus ? frame.Focus : style.Center);
            var farthest = Farthest(half, center);

            var a = Vector4.zero;
            var b = Vector4.zero;
            var waves = false;

            switch (field) {
                case WipeField.Linear: {
                    var radians = style.Angle * Mathf.Deg2Rad;
                    var scaled = ScaledDirection(half, style.Angle);

                    a = new Vector4(scaled.x, scaled.y, style.Mirror ? 1f : 0f, 0);
                    b = new Vector4(Mathf.Cos(radians), -Mathf.Sin(radians), 0, 0);
                    waves = true;
                    break;
                }
                case WipeField.Radial: {
                    var rotation = (style.Rotation + style.Spin * 360f * frame.Coverage) * Mathf.Deg2Rad;

                    if (style.RadialShape == WipeRadialShape.Circle) {
                        a = new Vector4(center.x, center.y, 1f / farthest, 1f);
                        b = new Vector4(Mathf.PI * 0.25f, 0, 0, 0);
                    } else {
                        var points = style.RadialShape == WipeRadialShape.Star ? Mathf.Clamp(style.Points, 3, 12) : 4;
                        var inner = style.RadialShape == WipeRadialShape.Star ? Mathf.Clamp(style.Inner, 0.2f, 1f) : 1f;

                        if (style.RadialShape == WipeRadialShape.Square) {
                            rotation += Mathf.PI * 0.25f;
                        }

                        // A star is a line from each tip at radius 1 to each valley at the inner radius. With
                        // the valley on the line between two tips it is a regular polygon.
                        var sector = Mathf.PI / points;
                        var valley = inner * Mathf.Cos(sector);
                        var slope = (1f - valley * Mathf.Cos(sector)) / (valley * Mathf.Sin(sector));

                        // The metric is largest at a valley, so that is what reaches the farthest corner last.
                        a = new Vector4(center.x, center.y, valley / farthest, 0f);
                        b = new Vector4(sector, slope, rotation, 0);
                    }

                    waves = true;
                    break;
                }
                case WipeField.Angular:
                    a = new Vector4(center.x, center.y, Mathf.Max(1, style.Arms), style.Twist);
                    b = new Vector4(1f / farthest, style.Clockwise ? 1f : -1f, style.StartAngle / 360f, 0);
                    waves = true;
                    break;
                case WipeField.Ramp: {
                    var texture = style.Ramp.Texture;
                    var visible = Vector2.one;

                    if (style.RampFit == WipeRampFit.Fill) {
                        // The ramp keeps its aspect and fills the rect, so one axis is cropped
                        var textureAspect = (float) texture.width / Mathf.Max(texture.height, 1);

                        visible = frame.Aspect >= textureAspect
                            ? new Vector2(1f, textureAspect / frame.Aspect)
                            : new Vector2(frame.Aspect / textureAspect, 1f);
                    }

                    var uv = new Rect(0.5f - visible.x * 0.5f, 0.5f - visible.y * 0.5f, visible.x, visible.y);
                    style.Ramp.GetRange(uv, out var least, out var greatest);

                    // Stretches the wipe over the values that are on screen, so no part of it does nothing
                    var range = Mathf.Max(greatest - least, 1f / 255f);

                    a = new Vector4(visible.x / frame.Aspect, visible.y, least, 1f / range);
                    material.SetTexture(s_rampId, texture);
                    break;
                }
                case WipeField.Shape: {
                    var shape = style.Shape;
                    var rotation = (style.Rotation + style.Spin * 360f * frame.Coverage) * Mathf.Deg2Rad;

                    // Opens until the silhouette clears the rect, with room for the edge.
                    var open = shape.OpenScale(center, half, rotation) * 1.05f + style.Softness;

                    a = new Vector4(center.x, center.y, open, 0);
                    b = new Vector4(2f * shape.Reach, Mathf.Cos(rotation), Mathf.Sin(rotation), 0);
                    material.SetTexture(s_shapeId, shape.Texture);
                    break;
                }
            }

            material.SetVector(s_fieldAId, a);
            material.SetVector(s_fieldBId, b);

            // Around a radial field a whole number of waves meets itself.
            var frequency = field == WipeField.Radial ? Mathf.Round(style.WaveFrequency) : style.WaveFrequency;
            var amplitude = waves ? style.WaveAmplitude : 0f;

            material.SetVector(s_waveId, new Vector4(amplitude, frequency, style.Wave == WipeWave.Sine ? 1f : 0f, 0));
        }

        private static void ApplyRepeat(Material material, WipeStyle style, Vector2 half)
        {
            switch (style.EffectiveRepeat) {
                case WipeRepeat.Lanes: {
                    var radians = style.LaneAngle * Mathf.Deg2Rad;
                    var direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
                    var extent = Mathf.Max(Mathf.Abs(direction.x) * half.x + Mathf.Abs(direction.y) * half.y, 1e-4f);
                    var width = Mathf.Max(style.LaneWidth, 0.01f);
                    var slide = style.LaneElement == WipeLaneElement.Slide;

                    // A cap is as long as half the lane's width, in the same units as the slide.
                    var cap = slide && style.Cap != WipeCap.Flat ? width * 0.5f / (2f * extent) : 0f;

                    material.SetVector(s_repeatAId, new Vector4(direction.x, direction.y, width, 1f / extent));
                    material.SetVector(s_repeatBId, new Vector4(style.Spread, style.Random, style.Alternate ? 1f : 0f, slide ? 1f : 0f));
                    material.SetVector(s_repeatCId, new Vector4((float) style.Cap, cap, 0, 0));
                    break;
                }
                case WipeRepeat.Grid: {
                    var radians = style.Diagonal ? Mathf.PI * 0.25f : 0f;

                    material.SetVector(s_repeatAId, new Vector4(Mathf.Max(style.GridSize, 0.01f), Mathf.Cos(radians), Mathf.Sin(radians), 0));
                    material.SetVector(s_repeatBId, new Vector4(style.Spread, style.Random, 0, (float) style.GridElement));
                    break;
                }
            }
        }

        /// <summary>
        /// A direction from an angle in degrees, 0 up and turning clockwise, divided by the rect's extent
        /// along it, so the dot product with a position runs from -1 to 1 across the rect.
        /// </summary>
        private static Vector2 ScaledDirection(Vector2 half, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            var extent = Mathf.Abs(direction.x) * half.x + Mathf.Abs(direction.y) * half.y;
            return direction / Mathf.Max(extent, 1e-4f);
        }

        /// <summary>
        /// The longest distance from a point to the rect's corners.
        /// </summary>
        private static float Farthest(Vector2 half, Vector2 center)
        {
            var farthest = 1e-4f;

            for (var i = 0; i < 4; i++) {
                var corner = new Vector2((i & 1) == 0 ? -half.x : half.x, (i & 2) == 0 ? -half.y : half.y);
                farthest = Mathf.Max(farthest, (corner - center).magnitude);
            }

            return farthest;
        }

        /// <summary>
        /// A position in the rect, from (0, 0) at the bottom left to (1, 1) at the top right, in rect
        /// heights from the center.
        /// </summary>
        private static Vector2 FromNormalized(float aspect, Vector2 normalized)
        {
            return (normalized - new Vector2(0.5f, 0.5f)) * new Vector2(aspect, 1f);
        }
    }
}
