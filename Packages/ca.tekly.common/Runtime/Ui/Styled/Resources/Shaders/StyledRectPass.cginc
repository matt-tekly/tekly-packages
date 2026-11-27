// The vertex and fragment programs shared by the Styled Rect shaders.
//
// "UI/Styled Rect" is the Core tier. "UI/Styled Rect Extended" defines STYLED_EXTENDED before including
// this, which adds what needs the position along the shape's edge: dashed strokes, strokes whose width
// wobbles, and a shape whose whole edge wobbles. A rect only uses the Extended shader when it has one of
// those, so everything else never pays for them.

#ifndef STYLED_RECT_PASS_INCLUDED
#define STYLED_RECT_PASS_INCLUDED

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
    float4 uv0        : TEXCOORD0; // xy = shape space position, z = shape address, w = layer address
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 local      : TEXCOORD0; // xy = shape space position, zw = texture uv
    float4 mask       : TEXCOORD1;
    // Everything below is constant across a quad.
    nointerpolation float4 shape      : TEXCOORD2; // xy = half size, z = band start, w = band end
    nointerpolation float4 radii      : TEXCOORD3; // top-left, top-right, bottom-right, bottom-left
    nointerpolation half4 colorA      : TEXCOORD4; // premultiplied
    nointerpolation half4 colorB      : TEXCOORD5; // premultiplied
    // xy = the linear gradient's direction, scaled so it spans -0.5 to 0.5 across the shape. For a
    // bevel, the unscaled direction the light comes from. z = gradient type. w = gradient exponent.
    nointerpolation float4 gradient   : TEXCOORD6;
    // xy = shadow offset, z = soft width, w = shadow spread. A stroke has no offset or spread, so in the
    // Extended shader a stroke carries its own wobble here: x = drift, y = length, w = seed.
    nointerpolation float4 soft       : TEXCOORD7;
    // x = the least radius the corners have for an inner shadow, a blurred fill or an Edge gradient (their
    // soft radii are the radii raised to at least this). y = the corners' exponent: 2 is circular, 4 a
    // squircle. z = reveal mode. w = 1 when the layer is textured.
    nointerpolation float4 corner     : TEXCOORD8;
    // x = layer kind. y = the one flag the kind reads: for a fill or stroke 1 when it is an underlay, for a
    // drop shadow 1 when it shows behind the shape. zw = gradient range start, end.
    // The flags are unpacked here rather than per pixel.
    nointerpolation float4 kind       : TEXCOORD9;
    // By reveal mode. Slide: xy = how far the shape's copy is moved. Straight: xy = direction, z = position.
    // Radial: xy = start ray, zw = end ray, signed so that a positive cross product is revealed.
    nointerpolation float4 reveal     : TEXCOORD10;
    nointerpolation float4 uvLimits   : TEXCOORD11; // min x, min y, max x, max y the texture uv may reach
#ifdef STYLED_EXTENDED
    nointerpolation float4 wobble     : TEXCOORD12; // shape wobble: amount, lobe length, seed, changes per second
    // dash spacing (0 = solid), dash length as a fraction of the spacing plus 2 for round caps,
    // dash speed, how much the stroke's width wobbles. The rest of a stroke's wobble is in `soft`.
    nointerpolation float4 stroke     : TEXCOORD13;
#endif
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert(Attributes IN)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    OUT.positionCS = UnityObjectToClipPos(IN.positionOS);

    float shape = round(IN.uv0.z);
    float layer = round(IN.uv0.w);

    float2 halfSize = StyledLoad(shape + STYLED_SHAPE_SIZE).xy;
    float4 header = StyledLoad(layer + STYLED_LAYER_HEADER); // kind + 8 * flags, soft rounding, band start, band end
    float4 gradient = StyledLoad(layer + STYLED_LAYER_GRADIENT); // type, angle, range start, range end

    // The kind and the flags share one value: the kind is what is left under a multiple of 8.
    float flags = floor(round(header.x) / 8.0);
    float layerKind = round(header.x) - flags * 8.0;

    OUT.shape = float4(halfSize, header.zw);
    OUT.radii = StyledLoad(shape + STYLED_SHAPE_RADII);

    // Flags: 1 = underlay, 2 = show behind, 4 = textured, 8 = ignore tint, 16 = ignore reveal.
    float ignoreReveal = floor(flags / 16.0);
    flags -= ignoreReveal * 16.0;
    float ignoreTint = floor(flags / 8.0);
    flags -= ignoreTint * 8.0;
    float textured = floor(flags / 4.0);
    flags -= textured * 4.0;
    float showBehind = floor(flags / 2.0);
    float underlay = flags - showBehind * 2.0;

    // The vertex color is the rect's tint times whatever colors the whole graphic. A layer that
    // ignores the tint keeps its own colors but still fades with the tint's alpha.
    // The layer colors are linear by now in a linear project, so a vertex color the canvas keeps in
    // gamma (its Vertex Color Always In Gamma Color Space option) is brought to linear too.
    float3 tintRgb = IN.color.rgb;

    if (_UIVertexColorAlwaysGammaSpace > 0.5 && !IsGammaSpace()) {
        tintRgb = GammaToLinearSpace(tintRgb);
    }

    float4 tint = float4(ignoreTint > 0.5 ? float3(1, 1, 1) : tintRgb, IN.color.a);

    OUT.colorA = StyledPrepareColor(StyledLoad(layer + STYLED_LAYER_COLOR_A), tint);
    OUT.colorB = StyledPrepareColor(StyledLoad(layer + STYLED_LAYER_COLOR_B), tint);

    // Angles follow CSS: 0 points up and they turn clockwise.
    float2 direction = float2(sin(gradient.y), cos(gradient.y));
    float extent = abs(direction.x) * halfSize.x + abs(direction.y) * halfSize.y;
    float2 scaledDirection = direction / max(2.0 * extent, 1e-4);

    // A row that never had an exponent written reads as 0, which means a straight gradient.
    float4 curveDashes = StyledLoad(layer + STYLED_LAYER_CURVE_DASHES); // gradient exponent, then a stroke's dashes
    float exponent = curveDashes.x;
    exponent = exponent > 0.0 ? exponent : 1.0;

    OUT.gradient = float4(layerKind > STYLED_KIND_BEVEL - 0.5 ? direction : scaledDirection, round(gradient.x), exponent);
    OUT.soft = StyledLoad(layer + STYLED_LAYER_SHADOW);

    // Soft radii are the radii raised to a least value, so that least value is all that is stored.
    float softRounding = header.y;

    // Reveal: mode, then by mode. A layer that ignores the reveal draws whole.
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
    }

    OUT.corner = float4(softRounding, StyledLoad(shape + STYLED_SHAPE_CORNER_EXPONENTS).x, revealMode, textured);
    OUT.kind = float4(layerKind, layerKind < STYLED_KIND_SHADOW - 0.5 ? underlay : showBehind, gradient.zw);

#ifdef STYLED_EXTENDED
    OUT.wobble = StyledLoad(shape + STYLED_SHAPE_WOBBLE);
    OUT.stroke = float4(curveDashes.yzw, 0.0);

    if (layerKind > STYLED_KIND_STROKE - 0.5 && layerKind < STYLED_KIND_SHADOW - 0.5) {
        float4 strokeWobble = StyledLoad(layer + STYLED_LAYER_STROKE_WOBBLE); // width, drift, length, seed
        OUT.stroke.w = strokeWobble.x;
        OUT.soft = float4(strokeWobble.y, strokeWobble.z, OUT.soft.z, strokeWobble.w);
    }
#endif

    // The texture mapping is linear in the position, so it is worked out per vertex. Scrolling runs
    // here too: no mesh rebuilds, and frac() keeps the offset small. The jump by a whole tile is
    // invisible on a repeating texture.
    float2 uv = float2(0, 0);
    OUT.uvLimits = float4(0, 0, 0, 0);

    if (textured > 0.5) {
        OUT.uvLimits = StyledLoad(shape + STYLED_SHAPE_TEXTURE_LIMITS);

        float4 matrixRows = StyledLoad(shape + STYLED_SHAPE_TEXTURE_MATRIX);
        float4 offsetScroll = StyledLoad(shape + STYLED_SHAPE_TEXTURE_OFFSET);

        // A drop shadow carries the texture along with its offset.
        float2 position = IN.uv0.xy;
        if (layerKind > STYLED_KIND_SHADOW - 0.5 && layerKind < STYLED_KIND_INNER_SHADOW - 0.5) {
            position -= OUT.soft.xy;
        }

        uv = float2(dot(matrixRows.xy, position), dot(matrixRows.zw, position)) + offsetScroll.xy + frac(_Time.y * offsetScroll.zw);
    }

    OUT.local = float4(IN.uv0.xy, uv);

    float2 pixelSize = OUT.positionCS.w;
    pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

    float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
    OUT.mask = float4(
        IN.positionOS.xy * 2.0 - clampedRect.xy - clampedRect.zw,
        0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy))
    );

    return OUT;
}

// Stretches a gradient position over its range: Color before the start, Color 2 after the end.
// Then bends it with the bias, so the halfway mix lands where the bias puts it. An exponent of 1
// leaves it straight.
float ApplyRange(float t, float2 range, float exponent)
{
    t = saturate((t - range.x) / max(range.y - range.x, 1e-4));
    return exponent != 1.0 && t > 0.0 ? pow(t, exponent) : t;
}

// A reach that always applies the wobble, for callers that use the distance everywhere.
#define STYLED_FULL_REACH 1e9

// The distance to the whole shape. In the Extended shader the shape's wobble moves the edge in and out,
// the same amount for every layer, so fills, strokes and shadows stay together. The wobble always follows
// the real shape's edge, whichever radii the distance itself uses.
// The caller only cares about the distance within `reach` of `focus`: past that its ramp is flat. The
// wobble moves the distance by at most its amount, so further out than that it cannot change the result
// and its noise is skipped. That spares the middle of a fill, which is most of a wobbling rect's pixels.
float WholeShapeDistance(Varyings IN, float2 position, float4 radii, float mitre, float focus, float reach)
{
    float distance = StyledShapeDistance(position, IN.shape.xy, radii, IN.corner.yyyy, mitre);

#ifdef STYLED_EXTENDED
    if (abs(distance - focus) <= IN.wobble.x + reach) {
        distance -= StyledShapeWobble(position, IN.shape.xy, IN.radii, IN.corner.yyyy, IN.wobble);
    }
#endif

    return distance;
}

// The distance every layer measures against. A slide reveal (a linear reveal with a Shape cap) is not a
// mask over the finished rect: it makes the shape itself smaller. What shows is where the shape overlaps
// a copy of itself moved back by the hidden distance, so the growing end has the shape's own corners, a
// stroke runs around it, a shadow is the shadow of what shows, and a bevel lights the new edge.
// `focus` and `reach` are passed on to WholeShapeDistance. A skipped wobble stays on the same flat side of
// the ramp, and so does the larger of the two distances.
float LayerDistance(Varyings IN, float2 position, float4 radii, float mitre, float focus, float reach)
{
    float distance = WholeShapeDistance(IN, position, radii, mitre, focus, reach);

    if (IN.corner.z > STYLED_REVEAL_SLIDE - 0.5 && IN.corner.z < STYLED_REVEAL_SLIDE + 0.5) {
        distance = max(distance, WholeShapeDistance(IN, position - IN.reveal.xy, radii, mitre, focus, reach));
    }

    return distance;
}

// How much of a pixel a straight or radial reveal shows, 0 to 1 with a one pixel edge. These cut the
// finished layers like a mask, so a ring revealed radially is a plain arc. 1 for no reveal and for a
// slide, which reshapes the rect instead.
float RevealMask(Varyings IN, float2 position, float pixel)
{
    float mode = IN.corner.z;

    if (mode < STYLED_REVEAL_STRAIGHT - 0.5) {
        return 1.0;
    }

    float inside;

    if (mode < STYLED_REVEAL_STRAIGHT + 0.5) {
        inside = IN.reveal.z - dot(position, IN.reveal.xy);
    } else if (mode < STYLED_REVEAL_RADIAL_WIDE + 0.5) {
        // A pie between two rays. Up to half a turn it is where both rays agree, beyond that where either does.
        float s0 = IN.reveal.x * position.y - IN.reveal.y * position.x;
        float s1 = IN.reveal.z * position.y - IN.reveal.w * position.x;
        inside = mode < STYLED_REVEAL_RADIAL + 0.5 ? min(s0, s1) : max(s0, s1);
    } else {
        // Nothing is revealed.
        return 0.0;
    }

    return saturate(inside / pixel + 0.5);
}

fixed4 frag(Varyings IN) : SV_Target
{
    float2 p = IN.local.xy;
    float2 halfSize = IN.shape.xy;

    // Taken outside the branch that samples the texture, where screen derivatives are not valid.
    float2 uv = IN.local.zw;
    float2 uvPerPixelX = ddx(uv);
    float2 uvPerPixelY = ddy(uv);
    float textured = IN.corner.w;
    float underlay = IN.kind.y; // read by fills and strokes only
    float showBehind = IN.kind.y; // read by drop shadows only
    float bandStart = IN.shape.z;
    float bandEnd = IN.shape.w;
    float kind = IN.kind.x;
    float gradientType = IN.gradient.z;

    float4 softRadii = max(IN.radii, IN.corner.x);
    float pixel = StyledPixelSize(p);
    float softWidth = max(IN.soft.z, pixel);

    // How much of the pixel the layer covers, and where it sits between Color (0) and Color 2 (1).
    float coverage;
    float t;

    if (kind < STYLED_KIND_SHADOW - 0.5) {
        // Fill or stroke: a band of the distance field.
        // A stroke keeps square corners square out to its outer edge.
        float mitre = max(bandEnd, 0.0);
        bool isStroke = bandStart > STYLED_NO_INNER_EDGE;

        // A blurred fill measures against the soft radii, so its soft edge does not crease into the
        // corners. Strokes have no soft edge.
        bool softFill = kind < STYLED_KIND_STROKE - 0.5 && IN.soft.z > 0.0;

        // A fill only has its outer edge, unless an Edge gradient reads the distance all the way in.
        // A stroke is drawn as a ring already, so nearly all of its pixels are near an edge.
        bool fullReach = isStroke || gradientType > STYLED_GRADIENT_EDGE - 0.5;
        float sd = LayerDistance(IN, p, softFill ? softRadii : IN.radii, mitre,
            bandEnd - underlay * pixel, fullReach ? STYLED_FULL_REACH : softWidth + pixel);

#ifdef STYLED_EXTENDED
        float dashed = isStroke && IN.stroke.x > 0.0 ? 1.0 : 0.0;
        float along = 0.0;
        float perimeter = 1.0;

        // A stroke's own wobble: its width varies and it drifts in and out, with its own length and seed.
        float wobbleWidth = IN.stroke.w;
        float wobbleDrift = IN.soft.x;

        if (isStroke && (dashed > 0.5 || wobbleWidth > 0.0 || wobbleDrift > 0.0)) {
            // Measured along the stroke's outer edge, its silhouette, so dashes keep their visible length
            // around corners.
            along = StyledEdgePosition(p, halfSize, IN.radii, IN.corner.yyyy, bandEnd, perimeter);

            // A length of 0 uses the shape's lobe length. Width and drift use different lobes, so the
            // stroke does not just swell where it moves out.
            float wobbleLength = IN.soft.y > 0.0 ? IN.soft.y : IN.wobble.y;
            float wobbleSeed = IN.wobble.z + IN.soft.w;
            float grow = wobbleWidth * StyledLobes(along, perimeter, wobbleLength, wobbleSeed + 21.0);
            float drift = wobbleDrift * StyledLobes(along, perimeter, wobbleLength, wobbleSeed + 40.0);

            bandStart += drift - grow;
            bandEnd = max(bandEnd + drift + grow, bandStart);
        }
#endif

        // An underlay stops a pixel short of its edge: an opaque stroke above ends there too, and two
        // anti-aliased edges in the same place would let this layer's color show through as a fringe.
        float outer = 1.0 - StyledRamp(sd - bandEnd + underlay * pixel, softWidth, pixel);
        float inner = isStroke ? saturate(0.5 + (sd - bandStart) / pixel) : 1.0;
        coverage = outer * inner;

#ifdef STYLED_EXTENDED
        if (dashed > 0.5) {
            coverage = StyledDashCoverage(sd, along, perimeter, bandStart, bandEnd, pixel, IN.stroke);
        } else if (isStroke) {
            // Fades out where the wobble leaves less than a pixel of width.
            coverage *= saturate((bandEnd - bandStart) / pixel);
        }
#endif

        float edgePosition = 0.0;

        if (gradientType > STYLED_GRADIENT_EDGE - 0.5) {
            // 0 at the layer's outer edge, 1 at its inner edge (or the deepest point of a fill).
            // Measured against the soft radii, so the gradient does not crease into the corners.
            float depth = isStroke ? bandEnd - bandStart : min(halfSize.x, halfSize.y) + bandEnd;
            float softDistance = LayerDistance(IN, p, softRadii, mitre, 0.0, STYLED_FULL_REACH);
            edgePosition = (bandEnd - softDistance) / max(depth, 1e-4);
        }

        coverage *= RevealMask(IN, p, pixel);
        t = ApplyRange(StyledGradientPosition(gradientType, p, halfSize, IN.gradient.xy, edgePosition), IN.kind.zw, IN.gradient.w);
    } else if (kind < STYLED_KIND_BEVEL - 0.5) {
        // Shadows carry their gradient along with their offset. An Edge gradient has no meaning
        // on a shadow and draws as Color.
        float2 q = p - IN.soft.xy;
        float spread = IN.soft.w;

        if (kind < STYLED_KIND_INNER_SHADOW - 0.5) {
            // Drop shadow: the shape moved by the offset and grown by the spread, with a soft edge.
            // Square corners stay square out to the spread.
            float sd = LayerDistance(IN, q, IN.radii, max(spread, 0.0), spread, softWidth + pixel);
            coverage = 1.0 - StyledRamp(sd - spread, softWidth, pixel);

            // Cut out under the shape, as CSS does. The cut sits on the inner half of the shape's
            // anti-aliased edge, so the shadow still backs that edge and no light fringe appears.
            // A shadow that shows behind the shape needs no cut, or the shape's distance.
            if (showBehind < 0.5) {
                float shapeDistance = LayerDistance(IN, p, IN.radii, 0.0, -0.5 * pixel, pixel);
                coverage *= saturate(shapeDistance / pixel + 1.0);
            }

            // A drop shadow is revealed in its own offset space, so it stays the shadow of what shows.
            coverage *= RevealMask(IN, q, pixel);
        } else {
            // Inner shadow: inside the real shape, shadowed where the offset shape, pushed in by
            // the spread, is not. That shape uses the soft radii so its corners do not crease.
            float shapeDistance = LayerDistance(IN, p, IN.radii, 0.0, 0.0, pixel);
            float sd = LayerDistance(IN, q, softRadii, 0.0, -spread, softWidth + pixel);
            coverage = saturate(0.5 - shapeDistance / pixel) * StyledRamp(sd + spread, softWidth, pixel);
            coverage *= RevealMask(IN, p, pixel);
        }

        t = ApplyRange(StyledGradientPosition(gradientType, q, halfSize, IN.gradient.xy, 0.0), IN.kind.zw, IN.gradient.w);
    } else {
        // Bevel: a band just inside the edge, lit by which way the edge faces. Color is the
        // highlight (facing the light) and Color 2 the shadow (facing away).
        float sd = LayerDistance(IN, p, IN.radii, 0.0, 0.0, STYLED_FULL_REACH);

        // On the growing end of a slide reveal the edge belongs to the shape's moved copy, so that is
        // where the lighting comes from.
        float2 normalAt = p;

        if (IN.corner.z > STYLED_REVEAL_SLIDE - 0.5 && IN.corner.z < STYLED_REVEAL_SLIDE + 0.5
            && WholeShapeDistance(IN, p - IN.reveal.xy, IN.radii, 0.0, 0.0, STYLED_FULL_REACH)
                > WholeShapeDistance(IN, p, IN.radii, 0.0, 0.0, STYLED_FULL_REACH)) {
            normalAt = p - IN.reveal.xy;
        }

        float facing = dot(StyledShapeNormal(normalAt, halfSize, IN.radii, IN.corner.yyyy), IN.gradient.xy);

        coverage = saturate(0.5 - sd / pixel) * StyledRamp(sd - bandStart, softWidth, pixel) * saturate(abs(facing) * 1.4142136);
        coverage *= RevealMask(IN, p, pixel);
        t = facing >= 0.0 ? 0.0 : 1.0;
    }

    // Both colors are premultiplied, so a gradient to a transparent color fades out cleanly.
    half4 color = lerp(IN.colorA, IN.colorB, t) * coverage;

    if (textured > 0.5) {
        // A Cover sprite stays inside its own part of the texture, so an atlas neighbour never shows.
        uv = clamp(uv, IN.uvLimits.xy, IN.uvLimits.zw);
        half4 texel = tex2Dgrad(_MainTex, uv, uvPerPixelX, uvPerPixelY) + _TextureSampleAdd;
        color *= half4(texel.rgb * texel.a, texel.a);
    }

    #ifdef UNITY_UI_CLIP_RECT
    half2 maskFactor = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
    color *= maskFactor.x * maskFactor.y;
    #endif

    #ifdef UNITY_UI_ALPHACLIP
    clip(color.a - 0.001);
    #endif

    // Shape each blend mode's output so that transparent pixels leave what is behind unchanged. The
    // keyword is set on the material by StyledRectStore, so no blend mode pays for another's output.
#if defined(STYLED_OUTPUT_MULTIPLY)
    // Faded toward white, lerp(1, straight color, alpha) written for a premultiplied color. An alpha of 1
    // leaves the alpha behind untouched under DstColor Zero.
    return fixed4(1.0 - color.a + color.rgb, 1.0);
#elif defined(STYLED_OUTPUT_ADD)
    // Additive and Screen. An alpha of 0 leaves the alpha behind untouched under their One blend.
    return fixed4(color.rgb, 0.0);
#else
    // Normal: premultiplied color with the coverage as alpha.
    return color;
#endif
}

#endif
