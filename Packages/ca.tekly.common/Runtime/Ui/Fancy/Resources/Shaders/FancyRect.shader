// Fancy Rect: a UI shape from one signed distance field, drawn as a stack of layers.
// FancyRect emits one quad per layer (shadows, fill, inner shadows, bevel, gloss, outlines), so every layer
// is a single cheap pass over its own quad and the whole element still batches with one material.
//
// Vertex layout (all packed values are integers below 2^24, which float32 stores exactly):
//   uv0.xy  local position relative to the shape center (unshifted)
//   uv0.zw  texture uv (computed on the CPU per vertex, so atlased sprites work)
//   uv1.x   radii TL | TR          (12-bit each, 0.25 px steps)
//   uv1.y   radii BR | BL
//   uv1.z   bulge top | bottom     (12-bit signed, 0.25 px steps)
//   uv1.w   bulge right | left
//   uv2.x   band start | end       (12-bit signed; start code 0 = no inner edge)
//           or, for solid layers (flag bit 23), texture scroll x | y (12-bit signed, 1/256 texture units/s)
//   uv2.y   softness | gradient angle
//   uv2.z   offset x | y           (12-bit signed)
//           or, for styled outlines (layer type 3), the packed outline style (see FancyRect.PackOutlineStyle)
//   uv2.w   flags: layer type (2 bits: band, inner shadow, bevel, styled band) | gradient type (3 bits) | corner types (4 x 2 bits) | textured (1 bit)
//           | channel check (1 bit, always set; draws magenta if the canvas drops the data)
//           | gradient bias (8 bits, 0-255 = -1 to 1) | solid (1 bit: band is the whole shape, uv2.x is scroll)
//   uv3.x   other color RGB (8 bits each); see color / tangent.w
//   uv3.y   other color alpha, relative to the anchor's | range start | range end   (8 bits each)
//   uv3.z   shape half size x | y   (12-bit each, 0.5 unit steps)
//   uv3.w   reveal: method (2 bits) | origin (2 bits) | clockwise / from-far-side (1 bit)
//           | value (16 bits: radial amount, or linear cut position in 1/16 units from -2048) | offset space (1 bit) | ignore renderer tint (1 bit) | anchor is color 2 (1 bit)
//   color   white with the anchor's alpha, so the CanvasRenderer color arrives untouched in rgb
//   tangent.w  anchor color RGB (8 bits each). The anchor is whichever gradient color has more alpha.
Shader "UI/Fancy Rect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // Set by FancyRect from its Blend Mode; defaults are Normal (premultiplied alpha).
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Op", Float) = 0
        // 0 = premultiplied color, 1 = color faded toward white (multiply/darken style)
        _BlendColorOutput ("Blend Color Output", Float) = 0
        // 0 = coverage alpha, 1 = always 0, 2 = always 1 (leaves destination alpha untouched)
        _BlendAlphaOutput ("Blend Alpha Output", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend [_SrcBlend] [_DstBlend]
        BlendOp [_BlendOp]
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // 3.5 for fwidth and the extra interpolator. WebGL2 / GLES3 and up.
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #define NO_INNER_EDGE -511.0

            #define LAYER_BAND 0
            #define LAYER_INNER_SHADOW 1
            #define LAYER_BEVEL 2
            #define LAYER_STYLED_BAND 3

            #define OUTLINE_DASHES 1
            #define OUTLINE_NOTCHES 2
            #define OUTLINE_WOBBLE 3

            #define REVEAL_NONE 0
            #define REVEAL_HORIZONTAL 1
            #define REVEAL_VERTICAL 2
            #define REVEAL_RADIAL 3

            #define GRADIENT_NONE 0
            #define GRADIENT_LINEAR 1
            #define GRADIENT_RADIAL 2
            #define GRADIENT_ANGULAR 3
            #define GRADIENT_EDGE 4

            #define CORNER_ROUND 0
            #define CORNER_CHAMFER 1
            #define CORNER_SQUIRCLE 2
            #define CORNER_INVERTED 3

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            fixed4 _Color;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            float _UIVertexColorAlwaysGammaSpace;
            float _BlendColorOutput;
            float _BlendAlphaOutput;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float4 uv0        : TEXCOORD0;
                float4 uv1        : TEXCOORD1;
                float4 uv2        : TEXCOORD2;
                float4 uv3        : TEXCOORD3;
                float4 tangent    : TANGENT; // w = anchor color RGB (8 bits each)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                fixed4 color      : COLOR;
                float4 local      : TEXCOORD0; // xy = position, zw = texture uv
                float4 radii      : TEXCOORD1; // TL, TR, BR, BL
                float4 bulge      : TEXCOORD2; // top, right, bottom, left
                float4 band       : TEXCOORD3; // start, end, softness, gradient angle (radians)
                // Packed integers must reach the fragment exactly; interpolation error at ~2^23 could flip low bits.
                nointerpolation float4 offset : TEXCOORD4; // xy = offset, z = flags, w = channel check ok
                float4 color2     : TEXCOORD5;
                float4 mask       : TEXCOORD6;
                float4 halfSize   : TEXCOORD7; // xy = half size, zw = gradient range start, end
                nointerpolation float4 extra : TEXCOORD8; // x = packed reveal, y = packed outline style
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 Unpack12(float packedValue)
            {
                float n = round(packedValue);
                float high = floor(n / 4096.0);
                return float2(high, n - high * 4096.0);
            }

            float2 UnpackUnsigned(float packedValue)
            {
                return Unpack12(packedValue) * 0.25;
            }

            float2 UnpackSigned(float packedValue)
            {
                return (Unpack12(packedValue) - 2048.0) * 0.25;
            }

            // Take `count` bits starting at `shift` from an integer stored in a float.
            float Bits(float value, float shift, float count)
            {
                return fmod(floor(value / exp2(shift)), exp2(count));
            }

            // Pushes the domain toward the center by the bulge amount. The push follows a cosine,
            // so it is largest mid-edge and zero at the corners, which keeps the corner shapes intact.
            float2 ApplyBulge(float2 p, float2 halfSize, float4 bulge)
            {
                float2 t = saturate(abs(p) / max(halfSize, 1e-4));
                float edgeX = cos(1.5707963 * t.y); // left/right push varies along y
                float edgeY = cos(1.5707963 * t.x); // top/bottom push varies along x

                float pushX = (p.x >= 0.0 ? bulge.y : bulge.w) * edgeX;
                float pushY = (p.y >= 0.0 ? bulge.x : bulge.z) * edgeY;

                return p - float2(p.x >= 0.0 ? pushX : -pushX, p.y >= 0.0 ? pushY : -pushY);
            }

            // Signed distance (negative inside) for one quadrant. `a` = abs(p) - halfSize.
            float CornerDistance(float2 a, float radius, float cornerType)
            {
                float boxDistance = length(max(a, 0.0)) + min(max(a.x, a.y), 0.0);

                if (radius <= 0.0) {
                    return boxDistance;
                }

                if (cornerType == CORNER_CHAMFER) {
                    return max(boxDistance, (a.x + a.y + radius) * 0.70710678);
                }

                if (cornerType == CORNER_INVERTED) {
                    return max(boxDistance, radius - length(a));
                }

                float2 q = a + radius;
                float inside = min(max(q.x, q.y), 0.0);
                float2 outside = max(q, 0.0);

                if (cornerType == CORNER_SQUIRCLE) {
                    float2 o2 = outside * outside;
                    return sqrt(sqrt(dot(o2, o2))) + inside - radius;
                }

                return length(outside) + inside - radius;
            }

            // Pick the corner by splitting between neighbouring corners at the midpoint of their circle centers
            // rather than at the center line, so unequal corners larger than half a side still meet cleanly.
            void PickCorner(float2 p, float4 radii, out bool top, out bool right)
            {
                float splitY = p.x >= 0.0 ? (radii.z - radii.y) * 0.5 : (radii.w - radii.x) * 0.5;
                top = p.y >= splitY;
                float splitX = top ? (radii.x - radii.y) * 0.5 : (radii.w - radii.z) * 0.5;
                right = p.x >= splitX;
            }

            float ShapeDistance(float2 p, float2 halfSize, float4 radii, float4 bulge, float4 cornerTypes)
            {
                p = ApplyBulge(p, halfSize, bulge);

                bool top;
                bool right;
                PickCorner(p, radii, top, right);

                float radius = top ? (right ? radii.y : radii.x) : (right ? radii.z : radii.w);
                float cornerType = top ? (right ? cornerTypes.y : cornerTypes.x) : (right ? cornerTypes.z : cornerTypes.w);

                // Mirror into the chosen corner (not abs(p)): near a shifted split the point can be on the other side.
                float2 mirrored = float2(right ? p.x : -p.x, top ? p.y : -p.y);
                return CornerDistance(mirrored - halfSize, radius, cornerType);
            }

            // 0-1 ramp across `width`, centered on the edge. Wider (soft) ramps are eased.
            float Ramp(float x, float width, float pixel)
            {
                float t = saturate(x / width + 0.5);
                return width > pixel * 1.5 ? t * t * (3.0 - 2.0 * t) : t;
            }

            float Wrap(float x, float m)
            {
                return x - m * floor(x / m);
            }

            // Length of a corner's edge for a given radius.
            float CornerArcLength(float radius, float cornerType)
            {
                if (cornerType == CORNER_CHAMFER) {
                    return radius * 1.4142136;
                }

                if (cornerType == CORNER_SQUIRCLE) {
                    return radius * 1.7544; // quarter of an n = 4 superellipse
                }

                return radius * 1.5707963; // round and inverted
            }

            // Distance along the outline, clockwise from the middle of the top side, plus the outline's total length.
            // Measured along the line `centerline` units from the shape edge (signed, negative inside), so corners
            // get that line's length: wider arcs outside the shape, tighter inside.
            // Where a corner's radius is smaller than the line's depth, the line has a sharp corner and the two sides
            // meet on the diagonal like a miter.
            // Measured on the unbulged shape, so spacing stretches slightly along bulged sides.
            // Corners are parameterized by angle, which is exact for round corners and close for the others.
            float EdgePosition(float2 p, float2 h, float4 radii, float4 bulge, float4 cornerTypes, float centerline, out float perimeter)
            {
                p = ApplyBulge(p, h, bulge);

                // Per corner: how far the centerline's straight sides stop short of the shape's corner, and the
                // radius of the centerline's corner arc (0 for a mitered corner).
                float4 inset = max(radii, -centerline);
                float4 curve = max(radii + centerline, 0.0);

                float4 arcs = float4(
                    CornerArcLength(curve.x, cornerTypes.x),
                    CornerArcLength(curve.y, cornerTypes.y),
                    CornerArcLength(curve.z, cornerTypes.z),
                    CornerArcLength(curve.w, cornerTypes.w));

                // Where each stretch of the centerline starts, going clockwise from the middle of the top side.
                float topRightCorner = h.x - inset.y;
                float rightSide = topRightCorner + arcs.y;
                float bottomRightCorner = rightSide + 2.0 * h.y - inset.y - inset.z;
                float bottomSide = bottomRightCorner + arcs.z;
                float bottomLeftCorner = bottomSide + 2.0 * h.x - inset.z - inset.w;
                float leftSide = bottomLeftCorner + arcs.w;
                float topLeftCorner = leftSide + 2.0 * h.y - inset.w - inset.x;
                float topSide = topLeftCorner + arcs.x;
                perimeter = max(topSide + h.x - inset.x, 1e-3);

                bool top;
                bool right;
                PickCorner(p, radii, top, right);

                float cornerInset = top ? (right ? inset.y : inset.x) : (right ? inset.z : inset.w);
                float cornerCurve = top ? (right ? curve.y : curve.x) : (right ? curve.z : curve.w);
                float2 a = float2(right ? p.x : -p.x, top ? p.y : -p.y) - h;
                float2 d = a + cornerInset; // relative to the center of the centerline's corner

                bool onArc = cornerCurve > 0.0 && d.x > 0.0 && d.y > 0.0;

                // Mitered corners split their two sides on the diagonal through the corner.
                bool horizontal = cornerCurve > 0.0 ? d.x <= 0.0 && (d.y > 0.0 || a.y >= a.x) : a.y >= a.x;

                float s;

                if (onArc) {
                    // 0 on the top or bottom side of the corner, 1 on its left or right side.
                    float f = atan2(d.x, d.y) / 1.5707963;
                    s = top
                        ? (right ? topRightCorner + f * arcs.y : topLeftCorner + (1.0 - f) * arcs.x)
                        : (right ? bottomRightCorner + (1.0 - f) * arcs.z : bottomLeftCorner + f * arcs.w);
                } else if (horizontal) {
                    s = top
                        ? (right ? p.x : topSide + p.x + h.x - inset.x)
                        : bottomSide + h.x - inset.z - p.x;
                } else {
                    s = right ? rightSide + h.y - inset.y - p.y : leftSide + p.y + h.y - inset.w;
                }

                // Near an off-center corner split a point can land just past either end.
                return Wrap(s, perimeter);
            }

            float EdgeHash(float i, float seed)
            {
                return frac(sin(i * 127.1 + seed * 311.7) * 43758.5453);
            }

            // Smooth value noise that repeats every `cells`, so it loops seamlessly around the edge.
            float LoopNoise(float x, float cells, float seed)
            {
                float i = floor(x);
                float f = frac(x);
                float h0 = EdgeHash(Wrap(i, cells), seed);
                float h1 = EdgeHash(Wrap(i + 1.0, cells), seed);
                return lerp(h0, h1, f * f * (3.0 - 2.0 * f)) * 2.0 - 1.0;
            }

            float BandCoverage(float sd, float bandStart, float bandEnd, float width, float pixel)
            {
                return (1.0 - Ramp(sd - bandEnd, width, pixel)) * Ramp(sd - bandStart, width, pixel);
            }

            // Coverage of an outline styled along the edge. `s` is the position along the edge, `perimeter` its length.
            // The style is packed by FancyRect.PackOutlineStyle. Repeats snap so a whole number fit around the edge.
            float StyledBandCoverage(float sd, float s, float perimeter, float bandStart, float bandEnd, float width, float pixel, float style)
            {
                float kind = Bits(style, 0, 2);
                float thickness = max(bandEnd - bandStart, 1e-3);
                float coverage;

                if (kind == OUTLINE_DASHES) {
                    float spacing = max(Bits(style, 2, 10) * 0.5, 1.0);
                    float dashFraction = Bits(style, 12, 6) / 63.0;
                    float speed = (Bits(style, 18, 5) - 16.0) * 8.0;
                    bool roundCaps = Bits(style, 23, 1) > 0.5;

                    float period = perimeter / max(1.0, floor(perimeter / spacing + 0.5));
                    float phase = period * frac(speed * _Time.y / period);
                    float halfDash = dashFraction * period * 0.5;

                    // Distance along the edge to the nearest dash center.
                    float along = abs(frac((s - phase - halfDash) / period + 0.5) - 0.5) * period;

                    // Round caps make each dash a capsule, with the caps inside the dash length. Zero length gives dots.
                    float halfThickness = thickness * 0.5;
                    float gap = max(along - max(halfDash - halfThickness, 0.0), 0.0);
                    float capsule = 1.0 - Ramp(length(float2(gap, sd - (bandStart + bandEnd) * 0.5)) - halfThickness, width, pixel);
                    float square = BandCoverage(sd, bandStart, bandEnd, width, pixel) * Ramp(halfDash - along, width, pixel);
                    coverage = roundCaps ? capsule : square;
                } else if (kind == OUTLINE_NOTCHES) {
                    float count = max(Bits(style, 2, 7), 1.0);
                    float halfWidth = max(Bits(style, 9, 6), 1.0) * 0.5;
                    float depth = Bits(style, 15, 4) / 15.0;
                    float shape = Bits(style, 19, 2);
                    bool innerSide = Bits(style, 21, 1) > 0.5;
                    float jitter = Bits(style, 22, 2) / 3.0;

                    float spacing = perimeter / count;
                    float nearest = floor(s / spacing + 0.5);
                    float cut = 0.0;

                    // Jitter can move a neighbour's notch closer than the nearest one, so check both sides.
                    for (int j = -1; j <= 1; j++) {
                        float k = nearest + j;
                        float index = Wrap(k, count);
                        float center = k * spacing + (EdgeHash(index, 3.0) - 0.5) * jitter * spacing * 0.6;
                        float depthScale = 1.0 - jitter * EdgeHash(index, 7.0) * 0.8;
                        float t = abs(s - center);

                        float profile;
                        if (shape < 0.5) {
                            profile = saturate(1.0 - t / halfWidth);
                        } else if (shape < 1.5) {
                            profile = saturate((halfWidth - t) / pixel + 0.5);
                        } else {
                            float r = t / halfWidth;
                            profile = sqrt(saturate(1.0 - r * r));
                        }

                        cut = max(cut, profile * depthScale);
                    }

                    cut *= depth * thickness;
                    bandStart += innerSide ? cut : 0.0;
                    bandEnd -= innerSide ? 0.0 : cut;

                    // Fades out where a notch leaves less than a pixel, so full-depth cuts leave no fringe.
                    coverage = BandCoverage(sd, bandStart, bandEnd, width, pixel) * saturate((bandEnd - bandStart) / pixel);
                } else {
                    // Wobble: two octaves of looping noise move the band (drift) and change its width (thickness).
                    float wavelength = max(Bits(style, 2, 8), 1.0);
                    float widthAmount = Bits(style, 10, 5) * 0.5;
                    float driftAmount = Bits(style, 15, 5) * 0.5;
                    float seed = Bits(style, 20, 4);

                    float cells = max(1.0, floor(perimeter / wavelength + 0.5));
                    float x = s / perimeter * cells;
                    float drift = (LoopNoise(x, cells, seed + 1.0) + 0.5 * LoopNoise(x * 2.0, cells * 2.0, seed + 9.0)) / 1.5;
                    float grow = (LoopNoise(x, cells, seed + 21.0) + 0.5 * LoopNoise(x * 2.0, cells * 2.0, seed + 33.0)) / 1.5;

                    float start = bandStart + driftAmount * drift - widthAmount * grow;
                    float end = max(bandEnd + driftAmount * drift + widthAmount * grow, start);
                    coverage = BandCoverage(sd, start, end, width, pixel) * saturate((end - start) / pixel);
                }

                return coverage;
            }

            float GradientT(float gradientType, float2 q, float2 halfSize, float angle, float sd, float bandStart, float bandEnd)
            {
                if (gradientType == GRADIENT_LINEAR) {
                    float2 dir = float2(cos(angle), sin(angle));
                    float extent = abs(dir.x) * halfSize.x + abs(dir.y) * halfSize.y;
                    return saturate(0.5 + dot(q, dir) / max(2.0 * extent, 1e-4));
                }

                if (gradientType == GRADIENT_RADIAL) {
                    return saturate(length(q / max(halfSize, 1e-4)));
                }

                if (gradientType == GRADIENT_ANGULAR) {
                    return frac(atan2(q.y, q.x) / 6.2831853 - angle / 6.2831853 + 1.0);
                }

                if (gradientType == GRADIENT_EDGE) {
                    // 0 at the layer's outer edge, 1 at its inner edge (or deepest point for fills).
                    float width = bandStart <= NO_INNER_EDGE ? min(halfSize.x, halfSize.y) + bandEnd : bandEnd - bandStart;
                    return saturate((bandEnd - sd) / max(width, 1e-4));
                }

                return 0.0;
            }

            // Remaps a 0-1 gradient position into [start, end] (solid colors outside it), then bends it so the
            // 50/50 mix lands at the midpoint. bias 0 = even, -1/1 = heavily favor Color / Color 2.
            float ShapeGradient(float t, float rangeStart, float rangeEnd, float bias)
            {
                t = saturate((t - rangeStart) / max(rangeEnd - rangeStart, 1e-4));

                float midpoint = 0.5 - bias * 0.4;
                return pow(t, log(0.5) / log(midpoint));
            }

            // How much of the element the Reveal setting shows at point c, 0-1 with a one-pixel anti-aliased edge.
            // Linear reveals arrive as a cut position already baked on the CPU (1/16 unit steps); radial reveals sweep
            // a pie around the center using two half-planes, intersected up to 180 degrees and unioned beyond.
            float RevealCoverage(float2 c, float packed, float pixel)
            {
                float method = Bits(packed, 0, 2);
                if (method == REVEAL_NONE) {
                    return 1.0;
                }

                float origin = Bits(packed, 2, 2);
                float flip = Bits(packed, 4, 1); // linear: reveal from the far side; radial: clockwise
                float value = Bits(packed, 5, 16);
                float s;

                if (method == REVEAL_HORIZONTAL || method == REVEAL_VERTICAL) {
                    float cut = value / 16.0 - 2048.0;
                    float coord = method == REVEAL_HORIZONTAL ? c.x : c.y;
                    s = flip > 0.5 ? coord - cut : cut - coord;
                } else {
                    // Origin: 0 top, 1 right, 2 bottom, 3 left.
                    float startAngle = origin < 0.5 ? 1.5707963 : (origin < 1.5 ? 0.0 : (origin < 2.5 ? 4.712389 : 3.1415927));
                    float sweep = value / 65535.0 * 6.2831853;
                    float endAngle = flip > 0.5 ? startAngle - sweep : startAngle + sweep;

                    float2 d0 = float2(cos(startAngle), sin(startAngle));
                    float2 d1 = float2(cos(endAngle), sin(endAngle));

                    // Signed distances to the start and end rays' lines, positive on the revealed side.
                    float s0 = flip > 0.5 ? c.x * d0.y - c.y * d0.x : d0.x * c.y - d0.y * c.x;
                    float s1 = flip > 0.5 ? d1.x * c.y - d1.y * c.x : c.x * d1.y - c.y * d1.x;
                    s = sweep <= 3.1415927 ? min(s0, s1) : max(s0, s1);
                }

                return saturate(s / pixel + 0.5);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = UnityObjectToClipPos(IN.positionOS);
                float flags = round(IN.uv2.w);
                bool solid = Bits(flags, 23, 1) > 0.5;
                bool styled = Bits(flags, 0, 2) == LAYER_STYLED_BAND;

                // Solid layers (fill, gloss, zero-spread shadows) cover the whole shape, so their band slot carries
                // the texture scroll instead. Scrolling runs here in the vertex shader: no mesh rebuilds.
                // frac() keeps the offset small; the jump by a whole tile is invisible on a repeating texture.
                float2 scroll = solid ? (Unpack12(IN.uv2.x) - 2048.0) / 256.0 : float2(0, 0);
                OUT.local = float4(IN.uv0.xy, IN.uv0.zw + frac(_Time.y * scroll));
                float revealBits = round(IN.uv3.w);
                // Strip the tint bits (22, 23) from the reveal. Styled outlines carry their style in place of an offset.
                OUT.extra = float4(fmod(revealBits, 4194304.0), styled ? round(IN.uv2.z) : 0.0, 0, 0);
                float gradientBits = round(IN.uv3.y);
                OUT.halfSize = float4(Unpack12(IN.uv3.z) * 0.5, Bits(gradientBits, 8, 8) / 255.0, Bits(gradientBits, 16, 8) / 255.0);

                OUT.radii = float4(UnpackUnsigned(IN.uv1.x), UnpackUnsigned(IN.uv1.y));

                float2 bulgeTopBottom = UnpackSigned(IN.uv1.z);
                float2 bulgeRightLeft = UnpackSigned(IN.uv1.w);
                OUT.bulge = float4(bulgeTopBottom.x, bulgeRightLeft.x, bulgeTopBottom.y, bulgeRightLeft.y);

                float2 bandRange = solid ? float2(-512.0, 0.0) : UnpackSigned(IN.uv2.x);
                float2 softAngle = Unpack12(IN.uv2.y);
                OUT.band = float4(bandRange, softAngle.x * 0.25, softAngle.y / 4095.0 * 6.2831853);

                OUT.offset = float4(styled ? float2(0, 0) : UnpackSigned(IN.uv2.z), flags, Bits(flags, 14, 1));

                // The vertex color is white with the anchor's alpha, so after the canvas multiplies in the
                // CanvasRenderer color, rgb is that color and a is anchor alpha * its alpha. Both layer colors are
                // packed: the anchor (more alpha) in tangent.w, the other in uv3 with alpha relative to the anchor.
                float anchorBits = round(IN.tangent.w);
                float otherBits = round(IN.uv3.x);
                float3 anchorRgb = float3(Bits(anchorBits, 16, 8), Bits(anchorBits, 8, 8), Bits(anchorBits, 0, 8)) / 255.0;
                float3 otherRgb = float3(Bits(otherBits, 16, 8), Bits(otherBits, 8, 8), Bits(otherBits, 0, 8)) / 255.0;
                float4 rendererColor = IN.color;

                // Canvas converts vertex colors to linear on the CPU unless "Vertex Color Always In Gamma" is on;
                // the layer colors are packed data, so they always need converting here.
                if (!IsGammaSpace()) {
                    anchorRgb = GammaToLinearSpace(anchorRgb);
                    otherRgb = GammaToLinearSpace(otherRgb);
                    if (_UIVertexColorAlwaysGammaSpace > 0.5) {
                        rendererColor.rgb = GammaToLinearSpace(rendererColor.rgb);
                    }
                }

                // Layers opted out of the renderer tint keep their colors but still fade with its alpha.
                float3 tintRgb = Bits(revealBits, 22, 1) > 0.5 ? float3(1, 1, 1) : rendererColor.rgb;
                float4 anchor = float4(anchorRgb * tintRgb, rendererColor.a);
                float4 other = float4(otherRgb * tintRgb, rendererColor.a * Bits(gradientBits, 0, 8) / 255.0);
                bool anchorIsColor2 = Bits(revealBits, 23, 1) > 0.5;

                OUT.color = (anchorIsColor2 ? other : anchor) * _Color;
                OUT.color2 = (anchorIsColor2 ? anchor : other) * _Color;

                float2 pixelSize = OUT.positionCS.w;
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                OUT.mask = float4(
                    IN.positionOS.xy * 2.0 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy))
                );

                return OUT;
            }

            fixed4 frag(Varyings IN) : SV_Target
            {
                if (IN.offset.w < 0.5) {
                    // Canvas is missing TexCoord1-3 (or drops their zw); make it obvious rather than subtly wrong.
                    return fixed4(1, 0, 1, 1);
                }

                float2 p = IN.local.xy;
                float2 halfSize = IN.halfSize.xy;

                float flags = round(IN.offset.z);
                float layerType = Bits(flags, 0, 2);
                float gradientType = Bits(flags, 2, 3);
                float4 cornerTypes = float4(Bits(flags, 5, 2), Bits(flags, 7, 2), Bits(flags, 9, 2), Bits(flags, 11, 2));
                float textured = Bits(flags, 13, 1);

                float bandStart = IN.band.x;
                float bandEnd = IN.band.y;
                float softness = IN.band.z;

                // Size of one screen pixel in local units, so edges stay ~1px wide at any canvas scale.
                float pixel = max(length(fwidth(p)) * 0.70710678, 1e-4);
                float width = max(softness, pixel);

                float2 q = p - IN.offset.xy;
                float sd = ShapeDistance(q, halfSize, IN.radii, IN.bulge, cornerTypes);

                float coverage;
                float t;

                if (layerType == LAYER_BEVEL) {
                    // Lit edge: the shape's outward normal (from the distance field's slope) against the light
                    // direction picks the highlight color (facing the light) or the shadow color (facing away).
                    float e = 0.5;
                    float2 slope = float2(
                        ShapeDistance(q + float2(e, 0), halfSize, IN.radii, IN.bulge, cornerTypes) - ShapeDistance(q - float2(e, 0), halfSize, IN.radii, IN.bulge, cornerTypes),
                        ShapeDistance(q + float2(0, e), halfSize, IN.radii, IN.bulge, cornerTypes) - ShapeDistance(q - float2(0, e), halfSize, IN.radii, IN.bulge, cornerTypes));
                    float2 normal = slope / max(length(slope), 1e-5);
                    float facing = dot(normal, float2(cos(IN.band.w), sin(IN.band.w)));

                    float insideShape = 1.0 - Ramp(sd, pixel, pixel);
                    float band = Ramp(sd - bandStart, width, pixel);
                    coverage = insideShape * band * saturate(abs(facing) * 1.4142136);
                    t = facing >= 0.0 ? 0.0 : 1.0;
                } else {
                    if (layerType == LAYER_INNER_SHADOW) {
                        // Inside the real shape, shadowed where the offset shape (shrunk by the spread) is not.
                        float insideShape = 1.0 - Ramp(ShapeDistance(p, halfSize, IN.radii, IN.bulge, cornerTypes), pixel, pixel);
                        coverage = insideShape * Ramp(sd + bandEnd, width, pixel);
                    } else if (layerType == LAYER_STYLED_BAND) {
                        float perimeter;
                        // Measured along the outline's outer edge, its silhouette, so dashes keep their visible length
                        // around corners. Their inner side gets shorter there instead.
                        float s = EdgePosition(q, halfSize, IN.radii, IN.bulge, cornerTypes, bandEnd, perimeter);
                        coverage = StyledBandCoverage(sd, s, perimeter, bandStart, bandEnd, width, pixel, round(IN.extra.y));
                    } else {
                        float outer = 1.0 - Ramp(sd - bandEnd, width, pixel);
                        float inner = bandStart <= NO_INNER_EDGE ? 1.0 : Ramp(sd - bandStart, width, pixel);
                        coverage = outer * inner;
                    }

                    t = GradientT(gradientType, q, halfSize, IN.band.w, sd, bandStart, bandEnd);
                    t = ShapeGradient(t, IN.halfSize.z, IN.halfSize.w, Bits(flags, 15, 8) / 255.0 * 2.0 - 1.0);
                }

                // Drop shadows are revealed in their own (offset) space so they stay the shadow of the revealed shape;
                // every other layer, including the inset gloss, uses the element's own space.
                float reveal = round(IN.extra.x);
                float2 revealPoint = Bits(reveal, 21, 1) > 0.5 ? q : p;
                coverage *= RevealCoverage(revealPoint, reveal, pixel);

                half4 color = lerp(IN.color, IN.color2, t);

                // Sampled for every layer and blended out when unused: the flag is constant per quad, and an
                // unconditional sample keeps the texture derivatives valid on every platform.
                half4 texel = tex2D(_MainTex, IN.local.zw) + _TextureSampleAdd;
                color *= lerp(half4(1, 1, 1, 1), texel, textured);
                color.a *= coverage;

                #ifdef UNITY_UI_CLIP_RECT
                half2 maskFactor = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= maskFactor.x * maskFactor.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                // Shape each blend mode's output so that transparent pixels leave the destination unchanged.
                float3 rgb = _BlendColorOutput > 0.5 ? lerp(float3(1, 1, 1), color.rgb, color.a) : color.rgb * color.a;
                float alpha = _BlendAlphaOutput > 1.5 ? 1.0 : (_BlendAlphaOutput > 0.5 ? 0.0 : color.a);

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
