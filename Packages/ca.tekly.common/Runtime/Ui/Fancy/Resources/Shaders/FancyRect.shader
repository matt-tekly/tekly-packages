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
//   uv2.w   flags: layer type (2 bits) | gradient type (3 bits) | corner types (4 x 2 bits) | textured (1 bit)
//           | channel check (1 bit, always set; draws magenta if the canvas drops the data)
//           | gradient bias (8 bits, 0-255 = -1 to 1) | solid (1 bit: band is the whole shape, uv2.x is scroll)
//   uv3.x   gradient color 2 RGB (8 bits each)
//   uv3.y   gradient color 2 alpha | range start | range end   (8 bits each)
//   uv3.z   shape half size x | y   (12-bit each, 0.5 unit steps)
//   uv3.w   reveal: method (2 bits) | origin (2 bits) | clockwise / from-far-side (1 bit)
//           | value (16 bits: radial amount, or linear cut position in 1/16 units from -2048) | offset space (1 bit)
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
                nointerpolation float4 extra : TEXCOORD8; // x = packed reveal
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

            float ShapeDistance(float2 p, float2 halfSize, float4 radii, float4 bulge, float4 cornerTypes)
            {
                p = ApplyBulge(p, halfSize, bulge);

                // Pick the corner by splitting between neighbouring corners at the midpoint of their circle centers
                // rather than at the center line, so unequal corners larger than half a side still meet cleanly.
                float splitY = p.x >= 0.0 ? (radii.z - radii.y) * 0.5 : (radii.w - radii.x) * 0.5;
                bool top = p.y >= splitY;
                float splitX = top ? (radii.x - radii.y) * 0.5 : (radii.w - radii.z) * 0.5;
                bool right = p.x >= splitX;

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

                // Solid layers (fill, gloss, zero-spread shadows) cover the whole shape, so their band slot carries
                // the texture scroll instead. Scrolling runs here in the vertex shader: no mesh rebuilds.
                // frac() keeps the offset small; the jump by a whole tile is invisible on a repeating texture.
                float2 scroll = solid ? (Unpack12(IN.uv2.x) - 2048.0) / 256.0 : float2(0, 0);
                OUT.local = float4(IN.uv0.xy, IN.uv0.zw + frac(_Time.y * scroll));
                OUT.extra = float4(round(IN.uv3.w), 0, 0, 0);
                float gradientBits = round(IN.uv3.y);
                OUT.halfSize = float4(Unpack12(IN.uv3.z) * 0.5, Bits(gradientBits, 8, 8) / 255.0, Bits(gradientBits, 16, 8) / 255.0);

                OUT.radii = float4(UnpackUnsigned(IN.uv1.x), UnpackUnsigned(IN.uv1.y));

                float2 bulgeTopBottom = UnpackSigned(IN.uv1.z);
                float2 bulgeRightLeft = UnpackSigned(IN.uv1.w);
                OUT.bulge = float4(bulgeTopBottom.x, bulgeRightLeft.x, bulgeTopBottom.y, bulgeRightLeft.y);

                float2 bandRange = solid ? float2(-512.0, 0.0) : UnpackSigned(IN.uv2.x);
                float2 softAngle = Unpack12(IN.uv2.y);
                OUT.band = float4(bandRange, softAngle.x * 0.25, softAngle.y / 4095.0 * 6.2831853);

                OUT.offset = float4(UnpackSigned(IN.uv2.z), flags, Bits(flags, 14, 1));

                float3 color2 = float3(Bits(round(IN.uv3.x), 16, 8), Bits(round(IN.uv3.x), 8, 8), Bits(round(IN.uv3.x), 0, 8)) / 255.0;
                float4 vertexColor = IN.color;

                // Canvas converts vertex colors to linear on the CPU unless "Vertex Color Always In Gamma" is on;
                // the gradient color is packed data, so it always needs converting here.
                if (!IsGammaSpace()) {
                    color2 = GammaToLinearSpace(color2);
                    if (_UIVertexColorAlwaysGammaSpace > 0.5) {
                        vertexColor.rgb = GammaToLinearSpace(vertexColor.rgb);
                    }
                }

                OUT.color = vertexColor * _Color;
                OUT.color2 = float4(color2, Bits(gradientBits, 0, 8) / 255.0) * _Color;

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
