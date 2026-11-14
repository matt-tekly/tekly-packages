// Fancy Rect: a UI shape from one signed distance field, drawn as a stack of layers.
// FancyRect emits one quad per layer (shadows, fill, inner shadows, bevel, gloss, outlines), so every layer
// is a single cheap pass over its own quad and the whole element still batches with one material.
// Everything that is constant across a quad is decoded in the vertex shader, so the fragment shader only
// does the per-pixel work.
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
//           | underlay (1 bit: an opaque layer above covers this layer's outer edge, so it ends a pixel early)
//           | gradient bias (8 bits, 0-255 = -1 to 1) | solid (1 bit: band is the whole shape, uv2.x is scroll)
//   uv3.x   other color RGB (8 bits each); see color / tangent.w
//   uv3.y   other color alpha, relative to the anchor's | range start | range end   (8 bits each)
//   uv3.z   shape half size x | y   (12-bit each: 3-bit exponent | 9-bit mantissa, 1/8 unit steps * 2^exponent).
//           Never zero, so a zero here means the canvas dropped the data and the shape draws magenta.
//           uv0.xy is scaled so the rect edge lands exactly on this rounded half size.
//   uv3.w   reveal: method (2 bits) | origin (2 bits) | clockwise / from-far-side (1 bit)
//           | value (16 bits: radial amount, or linear cut position as sign | 15 bits: 1/16 unit steps up to 1024,
//             then 0.5 unit steps) | offset space (1 bit) | ignore renderer tint (1 bit) | anchor is color 2 (1 bit)
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
                half4 color       : COLOR;     // premultiplied
                float4 local      : TEXCOORD0; // xy = position, zw = texture uv
                // Everything below except the mask is constant across a quad.
                nointerpolation float4 radii    : TEXCOORD1; // TL, TR, BR, BL
                nointerpolation float4 bulge    : TEXCOORD2; // top, right, bottom, left
                nointerpolation float4 band     : TEXCOORD3; // start, end, softness, gradient angle (radians)
                nointerpolation float4 offset   : TEXCOORD4; // xy = offset, z = channel check ok, w = reveal in offset space
                half4 color2      : TEXCOORD5; // premultiplied
                float4 mask       : TEXCOORD6;
                nointerpolation float4 halfSize : TEXCOORD7; // xy = half size, zw = gradient range start, end
                nointerpolation float4 extra    : TEXCOORD8; // x = reveal method (4 = radial past 180 degrees), y = packed outline style, z = linear reveal cut, w = underlay
                nointerpolation float4 corners  : TEXCOORD9; // corner types TL, TR, BR, BL
                nointerpolation float4 layer    : TEXCOORD10; // x = layer type, y = gradient type, z = textured, w = gradient bias exponent
                // Radial reveal: xy = start ray, zw = end ray, signed so cross(ray, point) > 0 is revealed. Linear: x = side.
                nointerpolation float4 rays     : TEXCOORD11;
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

            // One axis of the half size: 3-bit exponent | 9-bit mantissa.
            float DecodeHalfSize(float code)
            {
                float exponent = floor(code / 512.0);
                return (code - exponent * 512.0) * 0.125 * exp2(exponent);
            }

            // Pushes the domain toward the center by the bulge amount. The push follows a cosine,
            // so it is largest mid-edge and zero at the corners, which keeps the corner shapes intact.
            // Each side's push fades out close to the shape's own center line and never carries a point across it,
            // so two sides with different bulges still meet without a seam.
            // `warp` returns how the push changes along the edge: x = d(new x)/dy, y = d(new y)/dx.
            float2 ApplyBulge(float2 p, float2 halfSize, float4 bulge, out float2 warp)
            {
                float2 side = float2(p.x >= 0.0 ? 1.0 : -1.0, p.y >= 0.0 ? 1.0 : -1.0);
                float2 amount = float2(p.x >= 0.0 ? bulge.y : bulge.w, p.y >= 0.0 ? bulge.x : bulge.z);
                float2 safeHalf = max(halfSize, 1e-4);
                float2 fromCenter = abs(p);

                // The left/right push varies along y and the top/bottom push along x.
                float2 along = saturate(fromCenter.yx / safeHalf.yx);
                float2 sine;
                float2 cosine;
                sincos(1.5707963 * along, sine, cosine);

                // Inward bulges stop at 90% of the half size, so the edge itself is always past the fade.
                float2 strength = amount * saturate(fromCenter / (safeHalf * 0.1));

                float2 inRange = float2(fromCenter.y < safeHalf.y ? 1.0 : 0.0, fromCenter.x < safeHalf.x ? 1.0 : 0.0);
                warp = side.x * side.y * strength * sine * inRange * (1.5707963 / safeHalf.yx);

                return side * max(fromCenter - strength * cosine, 0.0);
            }

            float2 ApplyBulge(float2 p, float2 halfSize, float4 bulge)
            {
                float2 warp;
                return ApplyBulge(p, halfSize, bulge, warp);
            }

            // Which way the nearest side faces inside a box corner (both of `a` negative). Fans smoothly from one
            // side to the other around the corner instead of flipping on the diagonal.
            float2 NearestSide(float2 a)
            {
                return normalize(max(-a.yx, 1e-4));
            }

            // Signed distance (negative inside) for one quadrant. `a` = abs(p) - halfSize.
            // `slope` returns the field's gradient: its direction, and for squircles its length too, since their
            // norm is not a true distance.
            // `mitre` is how far outside the shape a square corner stays square, the way a stroke or a shadow's
            // spread keeps a sharp corner sharp. Past that the field rounds off, which is what a blur does.
            float CornerDistance(float2 a, float radius, float cornerType, float mitre, out float2 slope)
            {
                if (radius <= 0.0) {
                    float2 past = max(a - mitre, 0.0);
                    float pastLength = length(past);

                    if (pastLength > 0.0) {
                        slope = past / pastLength;
                        return pastLength + min(max(a.x, a.y) - mitre, 0.0) + mitre;
                    }

                    float nearest = max(a.x, a.y);
                    slope = nearest > 0.0 ? (a.x > a.y ? float2(1, 0) : float2(0, 1)) : NearestSide(a);
                    return nearest;
                }

                float2 beyond = max(a, 0.0);
                float beyondLength = length(beyond);
                float boxDistance = beyondLength + min(max(a.x, a.y), 0.0);
                float2 boxSlope = beyondLength > 0.0 ? beyond / beyondLength : NearestSide(a);

                if (cornerType == CORNER_CHAMFER) {
                    float chamfer = (a.x + a.y + radius) * 0.70710678;
                    slope = chamfer > boxDistance ? float2(0.70710678, 0.70710678) : boxSlope;
                    return max(boxDistance, chamfer);
                }

                if (cornerType == CORNER_INVERTED) {
                    float fromCorner = length(a);
                    float bite = radius - fromCorner;
                    slope = bite > boxDistance ? -a / max(fromCorner, 1e-5) : boxSlope;
                    return max(boxDistance, bite);
                }

                float2 q = a + radius;
                float inside = min(max(q.x, q.y), 0.0);
                float2 outside = max(q, 0.0);

                if (cornerType == CORNER_SQUIRCLE) {
                    float2 o2 = outside * outside;
                    float norm = sqrt(sqrt(dot(o2, o2)));
                    slope = norm > 0.0 ? outside * o2 / (norm * norm * norm) : NearestSide(q);
                    return norm + inside - radius;
                }

                float outsideLength = length(outside);
                slope = outsideLength > 0.0 ? outside / outsideLength : NearestSide(q);
                return outsideLength + inside - radius;
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

            float ShapeDistance(float2 p, float2 halfSize, float4 radii, float4 bulge, float4 cornerTypes, float mitre)
            {
                float2 warp;
                p = ApplyBulge(p, halfSize, bulge, warp);

                bool top;
                bool right;
                PickCorner(p, radii, top, right);

                float radius = top ? (right ? radii.y : radii.x) : (right ? radii.z : radii.w);
                float cornerType = top ? (right ? cornerTypes.y : cornerTypes.x) : (right ? cornerTypes.z : cornerTypes.w);

                // Mirror into the chosen corner (not abs(p)): near a shifted split the point can be on the other side.
                float2 mirror = float2(right ? 1.0 : -1.0, top ? 1.0 : -1.0);
                float2 slope;
                float sd = CornerDistance(p * mirror - halfSize, radius, cornerType, mitre, slope);
                slope *= mirror;

                // The bulge warp and the squircle norm stretch the field, which would make outlines thinner or
                // thicker along those edges. Dividing by the field's slope gives true distances along each side.
                // Through a corner the two sides' slopes are blended without their cross term: the exact slope
                // overshoots there, because a tight corner arc is far from the flat edge it assumes.
                float2 stretch = warp * slope;
                return sd / max(sqrt(dot(slope, slope) + dot(stretch, stretch)), 0.25);
            }

            // Deeper than a corner's radius the distance field turns that corner into a sharp mitre, which anything
            // soft that reaches that deep (an inner shadow, an Edge gradient) shows as a diagonal crease. A blur
            // rounds its corners instead, so those layers measure against corners at least as round as they are
            // deep. Bites stay as they are, and a square corner has no type of its own, so it rounds like a round one.
            void RoundDeepCorners(inout float4 radii, inout float4 cornerTypes, float2 halfSize, float depth)
            {
                float smallest = min(halfSize.x, halfSize.y);
                float largest = max(max(radii.x, radii.y), max(radii.z, radii.w));
                float rounding = min(depth, min(smallest, 2.0 * smallest - largest));

                bool4 bite = bool4(
                    radii.x > 0.0 && cornerTypes.x == CORNER_INVERTED,
                    radii.y > 0.0 && cornerTypes.y == CORNER_INVERTED,
                    radii.z > 0.0 && cornerTypes.z == CORNER_INVERTED,
                    radii.w > 0.0 && cornerTypes.w == CORNER_INVERTED);

                cornerTypes = radii > 0.0 ? cornerTypes : CORNER_ROUND;
                radii = bite ? radii : max(radii, rounding);
            }

            float ShapeDistance(float2 p, float2 halfSize, float4 radii, float4 bulge, float4 cornerTypes)
            {
                return ShapeDistance(p, halfSize, radii, bulge, cornerTypes, 0.0);
            }

            // 0-1 ramp centered on the edge. A sharp edge is a linear ramp one pixel wide. A soft edge has the
            // profile of a Gaussian blur, the falloff CSS and design tools use: `width` is four standard deviations,
            // which holds nearly all of the fade, with faint tails running another quarter of it past each end.
            float Ramp(float x, float width, float pixel)
            {
                if (width <= pixel * 1.5) {
                    return saturate(x / width + 0.5);
                }

                // erf(x / (sigma * sqrt(2))) with sigma = width / 4, approximated to within 0.0005.
                float z = x * 2.8284271 / width;
                float a = abs(z);
                float d = 1.0 + a * (0.278393 + a * (0.230389 + a * (0.000972 + a * 0.078108)));
                d *= d;
                d *= d;
                float erf = 1.0 - 1.0 / d;

                return 0.5 + 0.5 * (z < 0.0 ? -erf : erf);
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
                // A square corner stays square outside the shape too: the line's sides run on past the corner
                // (a negative inset) and meet on the diagonal.
                float4 square = step(radii, 0.0);
                float4 inset = lerp(max(radii, -centerline), -centerline, square);
                float4 curve = lerp(max(radii + centerline, 0.0), 0.0, square);

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
                    // Angles follow CSS: 0 points up and they turn clockwise.
                    float2 dir = float2(sin(angle), cos(angle));
                    float extent = abs(dir.x) * halfSize.x + abs(dir.y) * halfSize.y;
                    return saturate(0.5 + dot(q, dir) / max(2.0 * extent, 1e-4));
                }

                if (gradientType == GRADIENT_RADIAL) {
                    // An ellipse that reaches Color 2 at the rect's corners, so no part of the shape is left flat.
                    // Range End pulls it in: an inset of 0.29 ends it where it touches the sides.
                    return saturate(length(q / max(halfSize, 1e-4)) * 0.70710678);
                }

                if (gradientType == GRADIENT_ANGULAR) {
                    // Starts at the angle and sweeps clockwise, like a CSS conic gradient.
                    return frac((atan2(q.x, q.y) - angle) / 6.2831853);
                }

                if (gradientType == GRADIENT_EDGE) {
                    // 0 at the layer's outer edge, 1 at its inner edge (or deepest point for fills).
                    float width = bandStart <= NO_INNER_EDGE ? min(halfSize.x, halfSize.y) + bandEnd : bandEnd - bandStart;
                    return saturate((bandEnd - sd) / max(width, 1e-4));
                }

                return 0.0;
            }

            // Remaps a 0-1 gradient position into [start, end] (solid colors outside it), then bends it so the
            // 50/50 mix lands at the midpoint. The bend's exponent comes from the vertex shader.
            float ShapeGradient(float t, float rangeStart, float rangeEnd, float exponent)
            {
                t = saturate((t - rangeStart) / max(rangeEnd - rangeStart, 1e-4));
                return pow(t, exponent);
            }

            // bias 0 = even, -1/1 = heavily favor Color / Color 2.
            float GradientExponent(float bias)
            {
                float midpoint = 0.5 - bias * 0.4;
                return log(0.5) / log(midpoint);
            }

            // How much of the element the Reveal setting shows at point c, 0-1 with a one-pixel anti-aliased edge.
            // Decoded in the vertex shader: linear reveals are a cut position, radial reveals sweep a pie around
            // the center using two half-planes, intersected up to 180 degrees and unioned beyond.
            float RevealCoverage(float2 c, float method, float cut, float4 rays, float pixel)
            {
                if (method < 0.5) {
                    return 1.0;
                }

                float s;

                if (method < 2.5) {
                    float coord = method < 1.5 ? c.x : c.y;
                    s = rays.x * (cut - coord);
                } else {
                    float s0 = rays.x * c.y - rays.y * c.x;
                    float s1 = rays.z * c.y - rays.w * c.x;
                    s = method < 3.5 ? min(s0, s1) : max(s0, s1);
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
                float layerType = Bits(flags, 0, 2);
                bool solid = Bits(flags, 23, 1) > 0.5;
                bool styled = layerType == LAYER_STYLED_BAND;

                OUT.corners = float4(Bits(flags, 5, 2), Bits(flags, 7, 2), Bits(flags, 9, 2), Bits(flags, 11, 2));
                OUT.layer = float4(layerType, Bits(flags, 2, 3), Bits(flags, 13, 1), GradientExponent(Bits(flags, 15, 8) / 255.0 * 2.0 - 1.0));

                // Solid layers (fill, gloss, zero-spread shadows) cover the whole shape, so their band slot carries
                // the texture scroll instead. Scrolling runs here in the vertex shader: no mesh rebuilds.
                // frac() keeps the offset small; the jump by a whole tile is invisible on a repeating texture.
                float2 scroll = solid ? (Unpack12(IN.uv2.x) - 2048.0) / 256.0 : float2(0, 0);
                OUT.local = float4(IN.uv0.xy, IN.uv0.zw + frac(_Time.y * scroll));

                float gradientBits = round(IN.uv3.y);
                float2 halfSizeCodes = Unpack12(IN.uv3.z);
                OUT.halfSize = float4(DecodeHalfSize(halfSizeCodes.x), DecodeHalfSize(halfSizeCodes.y), Bits(gradientBits, 8, 8) / 255.0, Bits(gradientBits, 16, 8) / 255.0);

                OUT.radii = float4(UnpackUnsigned(IN.uv1.x), UnpackUnsigned(IN.uv1.y));

                float2 bulgeTopBottom = UnpackSigned(IN.uv1.z);
                float2 bulgeRightLeft = UnpackSigned(IN.uv1.w);
                OUT.bulge = float4(bulgeTopBottom.x, bulgeRightLeft.x, bulgeTopBottom.y, bulgeRightLeft.y);

                float2 bandRange = solid ? float2(-512.0, 0.0) : UnpackSigned(IN.uv2.x);
                float2 softAngle = Unpack12(IN.uv2.y);
                OUT.band = float4(bandRange, softAngle.x * 0.25, softAngle.y / 4095.0 * 6.2831853);

                // Reveal. Bits 22 and 23 of the same value are the tint bits, read further down.
                float revealBits = round(IN.uv3.w);
                float revealMethod = Bits(revealBits, 0, 2);
                float revealOrigin = Bits(revealBits, 2, 2);
                float revealFlip = Bits(revealBits, 4, 1) > 0.5 ? -1.0 : 1.0; // linear: reveal from the far side; radial: clockwise
                float revealValue = Bits(revealBits, 5, 16);

                // Linear cut: sign | 15 bits, 1/16 unit steps up to 1024 and 0.5 unit steps beyond.
                float cutNegative = revealValue >= 32768.0 ? 1.0 : 0.0;
                float cutCode = revealValue - 32768.0 * cutNegative;
                float cut = cutCode <= 16384.0 ? cutCode / 16.0 : 1024.0 + (cutCode - 16384.0) * 0.5;
                cut = cutNegative > 0.5 ? -cut : cut;

                OUT.rays = float4(revealFlip, 0, 0, 0);

                if (revealMethod == REVEAL_RADIAL) {
                    // Origin: 0 top, 1 right, 2 bottom, 3 left.
                    float startAngle = revealOrigin < 0.5 ? 1.5707963 : (revealOrigin < 1.5 ? 0.0 : (revealOrigin < 2.5 ? 4.712389 : 3.1415927));
                    float sweep = revealValue / 65535.0 * 6.2831853;
                    float endAngle = startAngle + revealFlip * sweep;

                    OUT.rays = float4(
                        revealFlip * float2(cos(startAngle), sin(startAngle)),
                        -revealFlip * float2(cos(endAngle), sin(endAngle)));
                    revealMethod = sweep <= 3.1415927 ? 3.0 : 4.0;
                }

                // Styled outlines carry their style in place of an offset.
                OUT.extra = float4(revealMethod, styled ? round(IN.uv2.z) : 0.0, cut, Bits(flags, 14, 1));
                OUT.offset = float4(styled ? float2(0, 0) : UnpackSigned(IN.uv2.z), IN.uv3.z > 0.5 ? 1.0 : 0.0, Bits(revealBits, 21, 1));

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
                float4 anchor = float4(anchorRgb * tintRgb, rendererColor.a) * _Color;
                float4 other = float4(otherRgb * tintRgb, rendererColor.a * Bits(gradientBits, 0, 8) / 255.0) * _Color;
                bool anchorIsColor2 = Bits(revealBits, 23, 1) > 0.5;

                // Premultiplied, so a gradient to a transparent color fades out instead of mixing in that color's rgb.
                anchor.rgb *= anchor.a;
                other.rgb *= other.a;

                OUT.color = anchorIsColor2 ? other : anchor;
                OUT.color2 = anchorIsColor2 ? anchor : other;

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
                if (IN.offset.z < 0.5) {
                    // Canvas is missing TexCoord1-3 (or drops their zw); make it obvious rather than subtly wrong.
                    return fixed4(1, 0, 1, 1);
                }

                float2 p = IN.local.xy;
                float2 halfSize = IN.halfSize.xy;

                float layerType = IN.layer.x;
                float4 cornerTypes = IN.corners;

                float bandStart = IN.band.x;
                float bandEnd = IN.band.y;
                float softness = IN.band.z;

                // Size of one screen pixel in local units, so edges stay ~1px wide at any canvas scale.
                // The root mean square of the two screen axes gives the same width at any rotation.
                float2 perPixelX = ddx(p);
                float2 perPixelY = ddy(p);
                float pixel = max(sqrt(0.5 * (dot(perPixelX, perPixelX) + dot(perPixelY, perPixelY))), 1e-4);
                float width = max(softness, pixel);

                float2 q = p - IN.offset.xy;

                float4 fieldRadii = IN.radii;
                float4 fieldTypes = cornerTypes;

                if (layerType == LAYER_INNER_SHADOW) {
                    RoundDeepCorners(fieldRadii, fieldTypes, halfSize, bandEnd + softness * 0.5);
                }

                // Outlines and drop shadows keep square corners square out to their outer edge (the spread, for a shadow).
                float mitre = layerType == LAYER_BAND || layerType == LAYER_STYLED_BAND ? max(bandEnd, 0.0) : 0.0;

                float sd = ShapeDistance(q, halfSize, fieldRadii, IN.bulge, fieldTypes, mitre);

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
                    // The light angle follows CSS like the gradient angles: 0 is straight above, turning clockwise.
                    float facing = dot(normal, float2(sin(IN.band.w), cos(IN.band.w)));

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
                        // An underlay stops a pixel short of its edge: an opaque layer above ends there too, and two
                        // anti-aliased edges in the same place would let this layer's color show through as a fringe.
                        float outer = 1.0 - Ramp(sd - bandEnd + IN.extra.w * pixel, width, pixel);
                        float inner = bandStart <= NO_INNER_EDGE ? 1.0 : Ramp(sd - bandStart, width, pixel);
                        coverage = outer * inner;
                    }

                    float gradientSd = sd;

                    if (IN.layer.y == GRADIENT_EDGE && layerType != LAYER_INNER_SHADOW) {
                        // The gradient runs into the shape, so it gets the same rounded corners as a soft inner
                        // shadow, as deep as the layer reaches inside the edge. The layer's own edge stays as it is.
                        float4 gradientRadii = IN.radii;
                        float4 gradientTypes = cornerTypes;
                        RoundDeepCorners(gradientRadii, gradientTypes, halfSize, max(-bandStart, 0.0));
                        gradientSd = ShapeDistance(q, halfSize, gradientRadii, IN.bulge, gradientTypes, mitre);
                    }

                    t = GradientT(IN.layer.y, q, halfSize, IN.band.w, gradientSd, bandStart, bandEnd);
                    t = ShapeGradient(t, IN.halfSize.z, IN.halfSize.w, IN.layer.w);
                }

                // Drop shadows are revealed in their own (offset) space so they stay the shadow of the revealed shape;
                // every other layer, including the inset gloss, uses the element's own space.
                float2 revealPoint = IN.offset.w > 0.5 ? q : p;
                coverage *= RevealCoverage(revealPoint, IN.extra.x, IN.extra.z, IN.rays, pixel);

                // Both colors are premultiplied.
                half4 color = lerp(IN.color, IN.color2, t);

                // Sampled for every layer and blended out when unused: the flag is constant per quad, and an
                // unconditional sample keeps the texture derivatives valid on every platform.
                half4 texel = tex2D(_MainTex, IN.local.zw) + _TextureSampleAdd;
                texel = lerp(half4(1, 1, 1, 1), texel, IN.layer.z);
                color *= half4(texel.rgb * texel.a, texel.a);
                color *= coverage;

                #ifdef UNITY_UI_CLIP_RECT
                half2 maskFactor = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color *= maskFactor.x * maskFactor.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                // Shape each blend mode's output so that transparent pixels leave the destination unchanged.
                // Faded toward white is lerp(1, straight color, alpha), written here for a premultiplied color.
                float3 rgb = _BlendColorOutput > 0.5 ? 1.0 - color.a + color.rgb : color.rgb;
                float alpha = _BlendAlphaOutput > 1.5 ? 1.0 : (_BlendAlphaOutput > 0.5 ? 0.0 : color.a);

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
