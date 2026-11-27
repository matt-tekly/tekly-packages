// The vertex and fragment programs of "UI/Styled Rect Solid", which draws the solid middle of a fill.
//
// A fill's middle is covered everywhere, so its pixels need no distance field: only the fill's color, its
// gradient, a straight or radial reveal mask and its texture. A GPU runs as many pixels at once as the
// registers of its shader allow, and a shader needs as many as its heaviest path, so the middles, which are
// most of a large rect's pixels, are drawn with this small shader instead of the full one. StyledRect puts
// them in a first submesh with this material, drawn before the rest (see StyledRect.UpdateGeometry).
//
// Vertices are the same as the full shader's: the layer address is negative, which this shader turns back.

#ifndef STYLED_RECT_SOLID_PASS_INCLUDED
#define STYLED_RECT_SOLID_PASS_INCLUDED

#include "UnityCG.cginc"
#include "UnityUI.cginc"
#include "StyledRectCore.cginc"

sampler2D _MainTex;
fixed4 _TextureSampleAdd;
float4 _ClipRect;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float _UIVertexColorAlwaysGammaSpace;

struct Attributes
{
    float4 positionOS : POSITION;
    float4 color      : COLOR;
    float4 uv0        : TEXCOORD0; // xy = shape space position, z = shape address, w = solid layer address
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    // x = position along a linear gradient, or y and z the scaled position for a radial one. w is unused.
    float4 gradientAt : TEXCOORD0;
    float4 uv         : TEXCOORD1; // xy = texture uv, zw = shape space position for the reveal mask
    float4 mask       : TEXCOORD2;
    nointerpolation half4 colorA   : TEXCOORD3; // premultiplied
    nointerpolation half4 colorB   : TEXCOORD4; // premultiplied
    // x = gradient type, y = gradient exponent, zw = gradient range start, end
    nointerpolation half4 gradient : TEXCOORD5;
    // x = reveal mode, y = 1 when textured
    nointerpolation half4 flags    : TEXCOORD6;
    // By reveal mode. Straight: xy = direction, z = position. Radial: xy = start ray, zw = end ray.
    nointerpolation float4 reveal  : TEXCOORD7;
    nointerpolation float4 uvLimits : TEXCOORD8;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert(Attributes IN)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    OUT.positionCS = UnityObjectToClipPos(IN.positionOS);

    float shape = round(IN.uv0.z);
    float layer = -round(IN.uv0.w) - 1.0;

    float2 halfSize = StyledLoad(shape + STYLED_SHAPE_SIZE).xy;
    float4 header = StyledLoad(layer + STYLED_LAYER_HEADER);
    float4 gradient = StyledLoad(layer + STYLED_LAYER_GRADIENT); // type, angle, range start, range end

    // Flags: 1 = underlay, 2 = show behind, 4 = textured, 8 = ignore tint, 16 = ignore reveal.
    float flags = floor(round(header.x) / 8.0);
    float ignoreReveal = floor(flags / 16.0);
    flags -= ignoreReveal * 16.0;
    float ignoreTint = floor(flags / 8.0);
    flags -= ignoreTint * 8.0;
    float textured = floor(flags / 4.0);

    // The vertex color is the rect's tint times whatever colors the whole graphic. A layer that ignores the
    // tint keeps its own colors but still fades with the tint's alpha.
    float3 tintRgb = IN.color.rgb;

    if (_UIVertexColorAlwaysGammaSpace > 0.5 && !IsGammaSpace()) {
        tintRgb = GammaToLinearSpace(tintRgb);
    }

    float4 tint = float4(ignoreTint > 0.5 ? float3(1, 1, 1) : tintRgb, IN.color.a);
    OUT.colorA = StyledPrepareColor(StyledLoad(layer + STYLED_LAYER_COLOR_A), tint);
    OUT.colorB = StyledPrepareColor(StyledLoad(layer + STYLED_LAYER_COLOR_B), tint);

    // The gradient position is linear in the position for a linear gradient, and the scaled position for a
    // radial one, so both interpolate. Angles follow CSS: 0 points up and they turn clockwise.
    float gradientType = round(gradient.x);
    float2 direction = float2(sin(gradient.y), cos(gradient.y));
    float extent = abs(direction.x) * halfSize.x + abs(direction.y) * halfSize.y;
    float linearAt = 0.5 + dot(IN.uv0.xy, direction / max(2.0 * extent, 1e-4));
    float2 radialAt = IN.uv0.xy * (0.70710678 / max(halfSize, 1e-4));
    OUT.gradientAt = float4(linearAt, radialAt, 0.0);

    float exponent = StyledLoad(layer + STYLED_LAYER_CURVE_DASHES).x;
    OUT.gradient = float4(gradientType, exponent > 0.0 ? exponent : 1.0, gradient.zw);

    // Reveal: a straight or radial mask. A slide reveal changes the shape, so a slid fill has no solid middle.
    float4 revealRow = StyledLoad(shape + STYLED_SHAPE_REVEAL);
    float revealMode = ignoreReveal > 0.5 ? 0.0 : round(revealRow.x);
    OUT.reveal = float4(revealRow.yzw, 0.0);

    if (revealMode > STYLED_REVEAL_RADIAL - 0.5 && revealMode < STYLED_REVEAL_RADIAL + 0.5) {
        // A pie around the center between two rays. Up to half a turn it is where both rays agree,
        // beyond that where either does.
        float start = revealRow.y;
        float sweep = revealRow.z;
        float turn = sweep < 0.0 ? -1.0 : 1.0;
        float end = start + sweep;

        OUT.reveal = float4(turn * cos(start), turn * sin(start), -turn * cos(end), -turn * sin(end));
        revealMode = abs(sweep) <= 3.1415927 ? STYLED_REVEAL_RADIAL : STYLED_REVEAL_RADIAL_WIDE;
    } else if (revealMode < STYLED_REVEAL_STRAIGHT - 0.5) {
        revealMode = 0.0;
    }

    OUT.flags = float4(revealMode, textured, 0.0, 0.0);

    // The texture mapping is linear in the position, so it is worked out per vertex. An untextured layer's
    // uv is a single point, so the sample below always hits the cache.
    float2 uv = float2(0, 0);
    OUT.uvLimits = float4(0, 0, 0, 0);

    if (textured > 0.5) {
        OUT.uvLimits = StyledLoad(shape + STYLED_SHAPE_TEXTURE_LIMITS);
        float4 matrixRows = StyledLoad(shape + STYLED_SHAPE_TEXTURE_MATRIX);
        float4 offsetScroll = StyledLoad(shape + STYLED_SHAPE_TEXTURE_OFFSET);
        uv = float2(dot(matrixRows.xy, IN.uv0.xy), dot(matrixRows.zw, IN.uv0.xy)) + offsetScroll.xy + frac(_Time.y * offsetScroll.zw);
    }

    OUT.uv = float4(uv, IN.uv0.xy);

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
    // Sampled outside any branch, so its screen derivatives are defined. A Cover sprite stays inside its own
    // part of the texture, so an atlas neighbour never shows.
    half4 texel = tex2D(_MainTex, clamp(IN.uv.xy, IN.uvLimits.xy, IN.uvLimits.zw)) + _TextureSampleAdd;

    // Where the pixel sits in the gradient, stretched over the range and bent by the bias.
    half t = 0.0;
    float type = IN.gradient.x;

    [branch]
    if (type > STYLED_GRADIENT_NONE + 0.5) {
        float raw = type < STYLED_GRADIENT_RADIAL - 0.5 ? IN.gradientAt.x : length(IN.gradientAt.yz);
        t = saturate((saturate(raw) - IN.gradient.z) / max(IN.gradient.w - IN.gradient.z, 1e-4));

        [branch]
        if (IN.gradient.y != 1.0) {
            t = t > 0.0 ? pow(t, IN.gradient.y) : t;
        }
    }

    // Both colors are premultiplied, so a gradient to a transparent color fades out cleanly.
    half4 color = lerp(IN.colorA, IN.colorB, t);

    // A straight or radial reveal cuts the fill like a mask, with a one pixel edge.
    float mode = IN.flags.x;
    float2 p = IN.uv.zw;
    float pixel = StyledPixelSize(p);

    [branch]
    if (mode > STYLED_REVEAL_STRAIGHT - 0.5) {
        // Nothing is revealed past the radial modes.
        float inside = -1e9;

        if (mode < STYLED_REVEAL_STRAIGHT + 0.5) {
            inside = IN.reveal.z - dot(p, IN.reveal.xy);
        } else if (mode < STYLED_REVEAL_RADIAL_WIDE + 0.5) {
            float s0 = IN.reveal.x * p.y - IN.reveal.y * p.x;
            float s1 = IN.reveal.z * p.y - IN.reveal.w * p.x;
            inside = mode < STYLED_REVEAL_RADIAL + 0.5 ? min(s0, s1) : max(s0, s1);
        }

        color *= saturate(inside / pixel + 0.5);
    }

    color *= IN.flags.y > 0.5 ? half4(texel.rgb * texel.a, texel.a) : half4(1, 1, 1, 1);

    #ifdef UNITY_UI_CLIP_RECT
    half2 maskFactor = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
    color *= maskFactor.x * maskFactor.y;
    #endif

    #ifdef UNITY_UI_ALPHACLIP
    clip(color.a - 0.001);
    #endif

    // Shape each blend mode's output so that transparent pixels leave what is behind unchanged.
#if defined(STYLED_OUTPUT_MULTIPLY)
    return fixed4(1.0 - color.a + color.rgb, 1.0);
#elif defined(STYLED_OUTPUT_ADD)
    return fixed4(color.rgb, 0.0);
#else
    return color;
#endif
}

#endif
