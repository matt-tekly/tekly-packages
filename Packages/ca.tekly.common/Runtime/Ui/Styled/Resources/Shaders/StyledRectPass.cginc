// The vertex and fragment programs shared by the Styled Rect shaders.
//
// "UI/Styled Rect" is the Core tier. "UI/Styled Rect Extended" defines STYLED_EXTENDED before including
// this, which adds what needs the position along the shape's edge: dashed strokes, strokes whose width
// wobbles, and a shape whose whole edge wobbles. A rect only uses the Extended shader when it has one of
// those, so everything else never pays for them.
//
// Mobile GPUs run fewer pixels at once the more registers a shader needs, and a shader needs as many as its
// heaviest path. So the fragment program has one path for every layer kind: each kind measures one or two
// distances from a copy of the shape, and its coverage is an edge on one of them times a one pixel cut on
// the other. The vertex program turns each layer into the codes that say which (see vert), and only one
// copy of the distance code is compiled. A fill's middle is drawn as its own quad, marked solid, and skips
// the distance field altogether (see StyledRect.OnPopulateMesh).

#ifndef STYLED_RECT_PASS_INCLUDED
#define STYLED_RECT_PASS_INCLUDED

#include "UnityCG.cginc"
#include "UnityUI.cginc"
#include "StyledRectCore.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
fixed4 _TextureSampleAdd;
float4 _ClipRect;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float _UIVertexColorAlwaysGammaSpace;

// Added to a textured layer's log2 texels per shape unit, so that it is never 0, which means untextured.
#define STYLED_TEXTURE_LEVEL_OFFSET 64.0

// How the two distances are taken (Varyings.distances.w), as bits.
#define STYLED_OFFSET_ON_SECOND 1.0 // the shadow's offset moves the second distance's copy, not the first's
#define STYLED_FIRST_SOFT 2.0       // the first distance uses the soft radii
#define STYLED_SECOND_SOFT 4.0      // the second distance uses the soft radii
#define STYLED_TAKE_SECOND 8.0      // the second distance is needed
#define STYLED_SECOND_MITRE 16.0    // the second distance keeps square corners square out to the mitre too

struct Attributes
{
    float4 positionOS : POSITION;
    float4 color      : COLOR;
    float4 uv0        : TEXCOORD0; // xy = shape space position, z = shape address, w = layer address
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Everything after `mask` is constant across a layer. Small values are half, which Mali keeps two to a
// register. The Extended shader has fifteen of these, the most GLES 3 guarantees.
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 local      : TEXCOORD0; // xy = shape space position, zw = texture uv
    float4 mask       : TEXCOORD1;
    nointerpolation float4 shape      : TEXCOORD2; // xy = half size, z = band start, w = band end
    nointerpolation float4 radii      : TEXCOORD3; // top-left, top-right, bottom-right, bottom-left
    nointerpolation half4 colorA      : TEXCOORD4; // premultiplied
    nointerpolation half4 colorB      : TEXCOORD5; // premultiplied
    // xy by gradient type. Linear: its direction, scaled so it spans -0.5 to 0.5 across the shape. Radial: one
    // over the half size. Edge: the band end and one over the depth the gradient spans. For a bevel, the
    // direction the light comes from. z = gradient type. w = gradient exponent.
    nointerpolation half4 gradient    : TEXCOORD6;
    // x = layer kind, or -1 for a fill's solid middle. y = soft width, or in the Extended shader a stroke's
    // drift. zw = gradient range start, end.
    nointerpolation half4 kind        : TEXCOORD7;
    // xy = a shadow's offset, or in the Extended shader a stroke's wobble length and seed. z = the first
    // distance's mitre. w = how the distances are taken, as STYLED_ bits.
    nointerpolation float4 distances  : TEXCOORD8;
    // x = which distance the edge is on: 1 or 2, negative to flip it, plus 10 for an underlay. y = the
    // edge's offset. z = which distance the cut is on: 0 for none, 1 or 2 with the cut half a pixel in, 3 on
    // the second with it a whole pixel in, negative to flip it. w = the cut's offset.
    nointerpolation half4 coverage    : TEXCOORD9;
    // x = the least radius the corners have for an inner shadow, a blurred fill or an Edge gradient (their
    // soft radii are the radii raised to at least this). y = the corners' exponent: 2 is circular, 4 a
    // squircle. z = reveal mode. w = 0 for an untextured layer, else its texture level (see vert).
    nointerpolation half4 corner      : TEXCOORD10;
    // By reveal mode. Slide: xy = how far the shape's copy is moved. Straight: xy = direction, z = position.
    // Radial: xy = start ray, zw = end ray, signed so that a positive cross product is revealed.
    nointerpolation float4 reveal     : TEXCOORD11;
    nointerpolation float4 uvLimits   : TEXCOORD12; // min x, min y, max x, max y the texture uv may reach
#ifdef STYLED_EXTENDED
    nointerpolation float4 wobble     : TEXCOORD13; // shape wobble: amount, lobe length, seed, changes per second
    // dash spacing (0 = solid), dash length as a fraction of the spacing plus 2 for round caps,
    // dash speed, how much the stroke's width wobbles.
    nointerpolation float4 stroke     : TEXCOORD14;
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

    // A fill's solid middle carries its layer address negative (see StyledRows.SolidAddress). Every pixel of
    // it is covered, so the fragment program skips the distance field there.
    float layer = round(IN.uv0.w);
    bool solid = layer < -0.5;
    layer = solid ? -layer - 1.0 : layer;

    float2 halfSize = StyledLoad(shape + STYLED_SHAPE_SIZE).xy;
    float4 header = StyledLoad(layer + STYLED_LAYER_HEADER); // kind + 8 * flags, soft rounding, band start, band end
    float4 gradient = StyledLoad(layer + STYLED_LAYER_GRADIENT); // type, angle, range start, range end
    float4 soft = StyledLoad(layer + STYLED_LAYER_SHADOW); // offset x, offset y, soft width, spread

    // The kind and the flags share one value: the kind is what is left under a multiple of 8.
    float flags = floor(round(header.x) / 8.0);
    float layerKind = round(header.x) - flags * 8.0;

    float bandStart = header.z;
    float bandEnd = header.w;
    float spread = soft.w;

    OUT.shape = float4(halfSize, bandStart, bandEnd);
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

    bool isFillOrStroke = layerKind < STYLED_KIND_SHADOW - 0.5;
    bool isStroke = bandStart > STYLED_NO_INNER_EDGE;
    bool isDropShadow = layerKind > STYLED_KIND_SHADOW - 0.5 && layerKind < STYLED_KIND_INNER_SHADOW - 0.5;
    bool isInnerShadow = layerKind > STYLED_KIND_INNER_SHADOW - 0.5 && layerKind < STYLED_KIND_BEVEL - 0.5;
    bool isBevel = layerKind > STYLED_KIND_BEVEL - 0.5;

    // Angles follow CSS: 0 points up and they turn clockwise.
    float gradientType = round(gradient.x);
    float2 direction = float2(sin(gradient.y), cos(gradient.y));
    float2 gradientAxis = direction / max(2.0 * (abs(direction.x) * halfSize.x + abs(direction.y) * halfSize.y), 1e-4);

    if (isBevel) {
        gradientAxis = direction;
    } else if (gradientType > STYLED_GRADIENT_EDGE - 0.5) {
        // 0 at the layer's outer edge, 1 at its inner edge (or the deepest point of a fill). An Edge gradient
        // has no meaning on a shadow and draws as Color.
        float depth = isStroke ? bandEnd - bandStart : min(halfSize.x, halfSize.y) + bandEnd;
        gradientAxis = isFillOrStroke ? float2(bandEnd, 1.0 / max(depth, 1e-4)) : float2(0, 0);
    } else if (gradientType > STYLED_GRADIENT_RADIAL - 0.5) {
        // An ellipse that reaches Color 2 at the rect's corners, so no part of the shape is left flat.
        gradientAxis = 0.70710678 / max(halfSize, 1e-4);
    }

    // A row that never had an exponent written reads as 0, which means a straight gradient.
    float4 curveDashes = StyledLoad(layer + STYLED_LAYER_CURVE_DASHES); // gradient exponent, then a stroke's dashes
    float exponent = curveDashes.x > 0.0 ? curveDashes.x : 1.0;

    OUT.gradient = float4(gradientAxis, gradientType, exponent);
    OUT.kind = float4(solid ? -1.0 : layerKind, soft.z, gradient.zw);

    // How each kind takes its distances and covers its pixels. d1 and d2 are the two distances.
    float distanceBits = 0.0;
    float mitre = 0.0;
    float4 coverage = float4(1.0, -bandEnd, 0.0, 0.0);

    if (isFillOrStroke) {
        // A band of d1. A stroke keeps square corners square out to its outer edge. A blurred fill measures
        // against the soft radii, so its soft edge does not crease into the corners. An Edge gradient reads
        // d2 against the soft radii too. An underlay stops a pixel short of its edge: an opaque stroke above
        // ends there too, and two anti-aliased edges in the same place would let this layer's color show
        // through as a fringe. A stroke also has an inner edge.
        mitre = max(bandEnd, 0.0);
        distanceBits = layerKind < STYLED_KIND_STROKE - 0.5 && soft.z > 0.0 ? STYLED_FIRST_SOFT : 0.0;
        distanceBits += gradientType > STYLED_GRADIENT_EDGE - 0.5 ? STYLED_TAKE_SECOND + STYLED_SECOND_SOFT + STYLED_SECOND_MITRE : 0.0;
        coverage = float4(underlay > 0.5 ? 11.0 : 1.0, -bandEnd, isStroke ? 1.0 : 0.0, -bandStart);
    } else if (isDropShadow) {
        // d1 is the distance from the shape moved by the offset and grown by the spread, with square corners
        // square out to the spread. It is cut out under the shape, as CSS does, unless it shows behind: d2 is
        // the shape's own distance. The cut sits on the inner half of the shape's anti-aliased edge, so the
        // shadow still backs that edge and no light fringe appears.
        mitre = max(spread, 0.0);
        distanceBits = showBehind > 0.5 ? 0.0 : STYLED_TAKE_SECOND;
        coverage = float4(1.0, -spread, showBehind > 0.5 ? 0.0 : 3.0, 0.0);
    } else if (isInnerShadow) {
        // Inside the shape (d1), where the offset shape pushed in by the spread (d2) is not. That shape uses
        // the soft radii so its corners do not crease.
        distanceBits = STYLED_OFFSET_ON_SECOND + STYLED_SECOND_SOFT + STYLED_TAKE_SECOND;
        coverage = float4(-2.0, -spread, -1.0, 0.0);
    } else {
        // Bevel: inside the shape (d1), as deep as its width, with a soft inner edge.
        coverage = float4(-1.0, bandStart, -1.0, 0.0);
    }

    OUT.distances = float4(soft.xy, mitre, distanceBits);
    OUT.coverage = coverage;

#ifdef STYLED_EXTENDED
    OUT.wobble = StyledLoad(shape + STYLED_SHAPE_WOBBLE);
    OUT.stroke = float4(0, 0, 0, 0);

    if (isFillOrStroke && isStroke) {
        // A stroke has no offset or soft edge, so its own wobble takes those places: drift in kind.y, and
        // its wobble's length (0 uses the shape's lobe length) and seed in distances.xy.
        float4 strokeWobble = StyledLoad(layer + STYLED_LAYER_STROKE_WOBBLE); // width, drift, length, seed
        OUT.stroke = float4(curveDashes.yzw, strokeWobble.x);
        OUT.kind.y = strokeWobble.y;
        OUT.distances.xy = float2(strokeWobble.z > 0.0 ? strokeWobble.z : OUT.wobble.y, OUT.wobble.z + strokeWobble.w);
    }
#endif

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

    // The texture mapping is linear in the position, so it is worked out per vertex. Scrolling runs
    // here too: no mesh rebuilds, and frac() keeps the offset small. The jump by a whole tile is
    // invisible on a repeating texture.
    float2 uv = float2(0, 0);
    float textureLevel = 0.0;
    OUT.uvLimits = float4(0, 0, 0, 0);

    if (textured > 0.5) {
        OUT.uvLimits = StyledLoad(shape + STYLED_SHAPE_TEXTURE_LIMITS);

        float4 matrixRows = StyledLoad(shape + STYLED_SHAPE_TEXTURE_MATRIX);
        float4 offsetScroll = StyledLoad(shape + STYLED_SHAPE_TEXTURE_OFFSET);

        // A drop shadow carries the texture along with its offset.
        float2 position = IN.uv0.xy - (isDropShadow ? soft.xy : float2(0, 0));
        uv = float2(dot(matrixRows.xy, position), dot(matrixRows.zw, position)) + offsetScroll.xy + frac(_Time.y * offsetScroll.zw);

        // The texture is sampled inside a branch, where screen derivatives are not defined, so its mip level
        // comes from texels per shape unit, worked out here, times units per pixel, worked out per pixel.
        // Stored as a log, offset so that a textured layer is never 0.
        float2 texelsAlongX = float2(matrixRows.x, matrixRows.z) * _MainTex_TexelSize.zw;
        float2 texelsAlongY = float2(matrixRows.y, matrixRows.w) * _MainTex_TexelSize.zw;
        textureLevel = log2(max(max(length(texelsAlongX), length(texelsAlongY)), 1e-6)) + STYLED_TEXTURE_LEVEL_OFFSET;
    }

    // Soft radii are the radii raised to a least value, so that least value is all that is stored.
    OUT.corner = float4(header.y, StyledLoad(shape + STYLED_SHAPE_CORNER_EXPONENTS).x, revealMode, textureLevel);
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

// The distance from a copy of the shape at `position`.
//
// A slide reveal (a linear reveal with a Shape cap) is not a mask over the finished rect: it makes the shape
// itself smaller. What shows is where the shape overlaps a copy of itself moved back by the hidden distance,
// so the growing end has the shape's own corners, a stroke runs around it, a shadow is the shadow of what
// shows, and a bevel lights the new edge. `copy` is which copy the distance belongs to: 1 for the moved one.
//
// In the Extended shader, with `wobbled`, each copy takes the shape's wobble at its own position, so the
// growing end wobbles as the shape does and the two copies meet where their wobbled edges do. Out come the
// winning copy's position along the edge, the edge's length and its wobble. `centerline` is the line the
// position is measured along (see StyledEdgePosition). Only the first distance is wobbled here, which keeps
// the noise to one copy per slide copy: it is by far the most code, and code costs every pixel registers.
float LayerDistance(Varyings IN, float2 position, float4 radii, float mitre, bool wobbled, bool needsAlong, float centerline,
    out float copy, out float along, out float perimeter, out float wobble)
{
    bool slide = IN.corner.z > STYLED_REVEAL_SLIDE - 0.5 && IN.corner.z < STYLED_REVEAL_SLIDE + 0.5;
    float distance = -1e9;
    copy = 0.0;
    along = 0.0;
    perimeter = 1.0;
    wobble = 0.0;

    [unroll]
    for (int i = 0; i < 2; i++) {
        [branch]
        if (i > 0 && !slide) {
            break;
        }

        float2 at = position - (i > 0 ? IN.reveal.xy : float2(0, 0));
        float d = StyledShapeDistance(at, IN.shape.xy, radii, IN.corner.yyyy, mitre);

#ifdef STYLED_EXTENDED
        float copyAlong = 0.0;
        float copyPerimeter = 1.0;
        float copyWobble = 0.0;

        [branch]
        if (wobbled && needsAlong) {
            copyAlong = StyledEdgePosition(at, IN.shape.xy, IN.radii, IN.corner.yyyy, centerline, copyPerimeter);
            copyWobble = IN.wobble.x > 0.0 ? StyledShapeWobbleAt(copyAlong, copyPerimeter, IN.wobble) : 0.0;
            d -= copyWobble;
        }

        bool wins = d > distance;
        along = wins ? copyAlong : along;
        perimeter = wins ? copyPerimeter : perimeter;
        wobble = wins ? copyWobble : wobble;
#endif

        copy = d > distance ? float(i) : copy;
        distance = max(distance, d);
    }

    return distance;
}

// Where a pixel sits in a layer's gradient, 0 at Color and 1 at Color 2, stretched over the range and bent by
// the bias. `position` has a shadow's offset taken off, and `secondDistance` is what an Edge gradient reads.
half GradientPosition(Varyings IN, float2 position, float secondDistance)
{
    float type = IN.gradient.z;
    half t = 0.0;

    [branch]
    if (type > STYLED_GRADIENT_NONE + 0.5) {
        float raw;

        if (type < STYLED_GRADIENT_RADIAL - 0.5) {
            raw = 0.5 + dot(position, IN.gradient.xy);
        } else if (type < STYLED_GRADIENT_EDGE - 0.5) {
            raw = length(position * IN.gradient.xy);
        } else {
            raw = (IN.gradient.x - secondDistance) * IN.gradient.y;
        }

        float2 range = IN.kind.zw;
        t = saturate((saturate(raw) - range.x) / max(range.y - range.x, 1e-4));

        [branch]
        if (IN.gradient.w != 1.0) {
            t = t > 0.0 ? pow(t, IN.gradient.w) : t;
        }
    }

    return t;
}

// How much of a pixel a straight or radial reveal shows, 0 to 1 with a one pixel edge. These cut the
// finished layers like a mask, so a ring revealed radially is a plain arc. 1 for no reveal and for a
// slide, which reshapes the rect instead.
half RevealMask(Varyings IN, float2 position, float pixel)
{
    float mode = IN.corner.z;
    half mask = 1.0;

    [branch]
    if (mode > STYLED_REVEAL_STRAIGHT - 0.5) {
        // Nothing is revealed past the radial modes.
        float inside = -1e9;

        if (mode < STYLED_REVEAL_STRAIGHT + 0.5) {
            inside = IN.reveal.z - dot(position, IN.reveal.xy);
        } else if (mode < STYLED_REVEAL_RADIAL_WIDE + 0.5) {
            // A pie between two rays. Up to half a turn it is where both rays agree, beyond that where either does.
            float s0 = IN.reveal.x * position.y - IN.reveal.y * position.x;
            float s1 = IN.reveal.z * position.y - IN.reveal.w * position.x;
            inside = mode < STYLED_REVEAL_RADIAL + 0.5 ? min(s0, s1) : max(s0, s1);
        }

        mask = saturate(inside / pixel + 0.5);
    }

    return mask;
}

// Takes one bit off the top of a set of STYLED_ bits, largest first.
bool TakeBit(inout float bits, float bit)
{
    bool set = bits > bit - 0.5;
    bits -= set ? bit : 0.0;
    return set;
}

fixed4 frag(Varyings IN) : SV_Target
{
    float2 p = IN.local.xy;

    // One screen pixel in shape space units, the same at any angle. Screen derivatives are only defined
    // outside branches, so it is taken here.
    float pixel = StyledPixelSize(p);
    float kind = IN.kind.x;

    // Shadows carry their gradient along with their offset, and a drop shadow its reveal too.
    bool isShadow = kind > STYLED_KIND_SHADOW - 0.5 && kind < STYLED_KIND_BEVEL - 0.5;
    bool isDropShadow = isShadow && kind < STYLED_KIND_INNER_SHADOW - 0.5;
    float2 offset = isShadow ? IN.distances.xy : float2(0, 0);

    half coverage = 1.0;
    float d1 = 0.0;
    float d2 = 0.0;
    float copy = 0.0;

    // A fill's solid middle is covered everywhere, which is most of a large rect's pixels.
    [branch]
    if (kind > -0.5) {
        float bits = IN.distances.w;
        bool secondMitre = TakeBit(bits, STYLED_SECOND_MITRE);
        bool takeSecond = TakeBit(bits, STYLED_TAKE_SECOND);
        bool secondSoft = TakeBit(bits, STYLED_SECOND_SOFT);
        bool firstSoft = TakeBit(bits, STYLED_FIRST_SOFT);
        bool offsetOnSecond = TakeBit(bits, STYLED_OFFSET_ON_SECOND);
        float4 softRadii = max(IN.radii, IN.corner.x);

        // A dashed stroke, or one with a wobble of its own, needs the position along the edge, as the shape's
        // wobble does. It measures along its outer edge, its silhouette, so dashes keep their visible length
        // around corners.
        bool strokeAlong = false;
        bool needsAlong = false;
#ifdef STYLED_EXTENDED
        float drift = IN.kind.y;
        strokeAlong = kind > STYLED_KIND_STROKE - 0.5 && kind < STYLED_KIND_SHADOW - 0.5
                      && (IN.stroke.x > 0.0 || IN.stroke.w > 0.0 || drift > 0.0);
        needsAlong = IN.wobble.x > 0.0 || strokeAlong;
#endif

        float along = 0.0;
        float perimeter = 1.0;
        float firstWobble = 0.0;

        // The one or two distances, as a loop so the distance code is written once. The shape's wobble moves
        // the edge in and out, the same amount for every layer, so fills, strokes and shadows stay together.
        // The first distance takes it exactly (see LayerDistance): it is the edge every kind but an inner
        // shadow draws. The second takes the first's, which differs from its own only by a shadow's offset
        // along the lobes.
        [unroll]
        for (int i = 0; i < 2; i++) {
            [branch]
            if (i > 0 && !takeSecond) {
                break;
            }

            bool second = i > 0;
            bool soft = second ? secondSoft : firstSoft;
            bool moved = second == offsetOnSecond;
            float copyOfThis;
            float alongOfThis;
            float perimeterOfThis;
            float wobbleOfThis;
            float d = LayerDistance(IN, p - (moved ? offset : float2(0, 0)), soft ? softRadii : IN.radii,
                second && !secondMitre ? 0.0 : IN.distances.z, !second, needsAlong, strokeAlong ? IN.shape.w : 0.0,
                copyOfThis, alongOfThis, perimeterOfThis, wobbleOfThis);

            d2 = second ? d - firstWobble : d2;
            d1 = second ? d1 : d;
            copy = second ? copy : copyOfThis;
            along = second ? along : alongOfThis;
            perimeter = second ? perimeter : perimeterOfThis;
            firstWobble = second ? firstWobble : wobbleOfThis;
        }

        // The edge: soft or sharp, on one distance, flipped or not. An underlay ends a pixel early.
        half edgeCode = IN.coverage.x;
        half underlay = abs(edgeCode) > 5.0 ? 1.0 : 0.0;
        float edgeDistance = (abs(edgeCode) - underlay * 10.0 > 1.5 ? d2 : d1) * sign(edgeCode);
        coverage = 1.0 - StyledRamp(edgeDistance + IN.coverage.y + underlay * pixel, max(IN.kind.y, pixel), pixel);

        // The cut: a pixel wide, on one distance, flipped or not.
        half cutCode = abs(IN.coverage.z);
        float cutDistance = (cutCode > 1.5 ? d2 : d1) * sign(IN.coverage.z);
        coverage *= cutCode < 0.5 ? 1.0 : saturate((cutDistance + IN.coverage.w) / pixel + (cutCode > 2.5 ? 1.0 : 0.5));

#ifdef STYLED_EXTENDED
        // A dashed stroke, or one with a wobble of its own: its width varies and it drifts in and out, with
        // its own length and seed.
        [branch]
        if (strokeAlong) {
            float bandStart = IN.shape.z;
            float bandEnd = IN.shape.w;

            // Width and drift use different lobes, so the stroke does not just swell where it moves out.
            float grow = IN.stroke.w * StyledLobes(along, perimeter, IN.distances.x, IN.distances.y + 21.0);
            float moved = drift * StyledLobes(along, perimeter, IN.distances.x, IN.distances.y + 40.0);
            bandStart += moved - grow;
            bandEnd = max(bandEnd + moved + grow, bandStart);

            if (IN.stroke.x > 0.0) {
                coverage = StyledDashCoverage(d1, along, perimeter, bandStart, bandEnd, pixel, IN.stroke);
            } else {
                // Fades out where the wobble leaves less than a pixel of width.
                coverage = (1.0 - StyledRamp(d1 - bandEnd, pixel, pixel)) * saturate(0.5 + (d1 - bandStart) / pixel)
                           * saturate((bandEnd - bandStart) / pixel);
            }
        }
#endif
    }

    half t = GradientPosition(IN, p - offset, d2);

    // A bevel is lit by which way the edge faces: Color (the highlight) where it faces the light, Color 2
    // (the shadow) where it faces away, fading out where it is side on. On the growing end of a slide reveal
    // the edge belongs to the shape's moved copy, so that is where the lighting comes from.
    [branch]
    if (kind > STYLED_KIND_BEVEL - 0.5) {
        float2 normalAt = p - (copy > 0.5 ? IN.reveal.xy : float2(0, 0));
        float facing = dot(StyledShapeNormal(normalAt, IN.shape.xy, IN.radii, IN.corner.yyyy), IN.gradient.xy);
        coverage *= saturate(abs(facing) * 1.4142136);
        t = facing >= 0.0 ? 0.0 : 1.0;
    }

    coverage *= RevealMask(IN, p - (isDropShadow ? offset : float2(0, 0)), pixel);

    // Both colors are premultiplied, so a gradient to a transparent color fades out cleanly.
    half4 color = lerp(IN.colorA, IN.colorB, t) * coverage;

    // Only a textured layer samples. Its mip level is its texels per shape unit, from the vertex program,
    // times shape units per pixel. A Cover sprite stays inside its own part of the texture, so an atlas
    // neighbour never shows.
    [branch]
    if (IN.corner.w > 0.5) {
        float level = max(IN.corner.w - STYLED_TEXTURE_LEVEL_OFFSET + log2(pixel), 0.0);
        float2 uv = clamp(IN.local.zw, IN.uvLimits.xy, IN.uvLimits.zw);
        half4 texel = tex2Dlod(_MainTex, float4(uv, 0.0, level)) + _TextureSampleAdd;
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
