// Shared by every Styled Rect shader: reading the data texture and the shape's distance field.
//
// A vertex carries the shape space position (uv0.xy) and two addresses into the data texture: the rect's
// shape (uv0.z) and the layer this quad draws (uv0.w). The layout of those texels is documented on StyledRows
// in StyledTypes.cs. Change both together.

#ifndef STYLED_RECT_CORE_INCLUDED
#define STYLED_RECT_CORE_INCLUDED

#define STYLED_SHAPE_SIZE 0.0
#define STYLED_SHAPE_RADII 1.0
#define STYLED_SHAPE_CORNER_EXPONENTS 2.0
#define STYLED_SHAPE_WOBBLE 3.0
#define STYLED_SHAPE_REVEAL 4.0

#define STYLED_REVEAL_SLIDE 1.0
#define STYLED_REVEAL_STRAIGHT 2.0
#define STYLED_REVEAL_RADIAL 3.0
#define STYLED_REVEAL_RADIAL_WIDE 4.0
#define STYLED_REVEAL_HIDDEN 5.0
#define STYLED_SHAPE_TEXTURE_MATRIX 5.0
#define STYLED_SHAPE_TEXTURE_OFFSET 6.0
#define STYLED_SHAPE_TEXTURE_LIMITS 7.0

#define STYLED_LAYER_HEADER 0.0
#define STYLED_LAYER_SHADOW 1.0
#define STYLED_LAYER_COLOR_A 2.0
#define STYLED_LAYER_COLOR_B 3.0
#define STYLED_LAYER_GRADIENT 4.0
#define STYLED_LAYER_CURVE_DASHES 5.0
#define STYLED_LAYER_STROKE_WOBBLE 6.0

#define STYLED_KIND_FILL 0.0
#define STYLED_KIND_STROKE 1.0
#define STYLED_KIND_SHADOW 2.0
#define STYLED_KIND_INNER_SHADOW 3.0
#define STYLED_KIND_BEVEL 4.0

#define STYLED_GRADIENT_NONE 0.0
#define STYLED_GRADIENT_LINEAR 1.0
#define STYLED_GRADIENT_RADIAL 2.0
#define STYLED_GRADIENT_EDGE 3.0

// Band start of a layer with no inner edge (StyledLayer.NO_INNER_EDGE is -1e9).
#define STYLED_NO_INNER_EDGE -1e8

Texture2D<float4> _TeklyStyledData;
float _TeklyStyledDataWidth;

float4 StyledLoad(float address)
{
    float row = floor(address / _TeklyStyledDataWidth);
    float column = address - row * _TeklyStyledDataWidth;
    return _TeklyStyledData.Load(int3(column, row, 0));
}

// Stored colors are gamma with straight alpha. The renderer tint arrives separately, in the vertex color.
// Returns the color premultiplied.
half4 StyledPrepareColor(float4 stored, float4 tint)
{
    if (!IsGammaSpace()) {
        stored.rgb = GammaToLinearSpace(stored.rgb);
    }

    stored *= tint;
    stored.rgb *= stored.a;
    return stored;
}

// Size of one screen pixel in shape space units, so edges stay about a pixel wide at any scale.
// The root mean square of the two screen axes gives the same width at any rotation.
float StyledPixelSize(float2 p)
{
    float2 perPixelX = ddx(p);
    float2 perPixelY = ddy(p);
    return max(sqrt(0.5 * (dot(perPixelX, perPixelX) + dot(perPixelY, perPixelY))), 1e-4);
}

// 0-1 ramp centered on an edge. A sharp edge is a linear ramp one pixel wide. A soft edge has the profile of
// a Gaussian blur, the falloff CSS and design tools use: `width` is four standard deviations, which holds
// nearly all of the fade, with faint tails running another quarter of it past each end.
float StyledRamp(float x, float width, float pixel)
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

// Picks the corner a point belongs to and returns the point relative to that corner of the rect, mirrored
// so the corner is up and to the right. Radii and exponents are top-left, top-right, bottom-right, bottom-left.
// The split between neighbouring corners is at the midpoint of their circle centers rather than at the
// center line, so unequal corners larger than half a side still meet cleanly.
float2 StyledCornerSpace(float2 p, float2 halfSize, float4 radii, float4 exponents, out float radius, out float exponent, out float2 mirror)
{
    float splitY = p.x >= 0.0 ? (radii.z - radii.y) * 0.5 : (radii.w - radii.x) * 0.5;
    bool top = p.y >= splitY;
    float splitX = top ? (radii.x - radii.y) * 0.5 : (radii.w - radii.z) * 0.5;
    bool right = p.x >= splitX;

    radius = top ? (right ? radii.y : radii.x) : (right ? radii.z : radii.w);
    exponent = top ? (right ? exponents.y : exponents.x) : (right ? exponents.z : exponents.w);

    // Mirror into the chosen corner (not abs(p)): near a shifted split the point can be on the other side.
    mirror = float2(right ? 1.0 : -1.0, top ? 1.0 : -1.0);
    return p * mirror - halfSize;
}

// A smoothed corner is a superellipse: |x|^n + |y|^n = r^n. Exponent 2 is a circle and 4 a squircle.
// `outside` is the point relative to the corner's center, both parts at least 0 and not both 0.
// Returns the superellipse's norm, and in `slope` the direction and rate the norm grows.
float StyledSuperellipse(float2 outside, float exponent, out float2 slope)
{
    float2 powered = pow(outside, exponent);
    float sum = max(powered.x + powered.y, 1e-12);
    float norm = pow(sum, 1.0 / exponent);

    // d(norm)/d(x) = x^(n-1) / norm^(n-1), written with the powers already taken.
    slope = powered * norm / (max(outside, 1e-6) * sum);
    return norm;
}

// Signed distance (negative inside) to a rect with a radius per corner. A true distance for circular
// corners, except outside a square corner (see mitre). For a smoothed corner the superellipse's norm is
// divided by its slope, which gives the true distance near the edge, so strokes keep their width around it.
// `mitre` is how far outside the shape a square corner stays square, the way a stroke keeps a sharp corner
// sharp. Past that the field rounds off.
float StyledShapeDistance(float2 p, float2 halfSize, float4 radii, float4 exponents, float mitre)
{
    float radius;
    float exponent;
    float2 mirror;
    float2 a = StyledCornerSpace(p, halfSize, radii, exponents, radius, exponent, mirror);

    if (radius <= 0.0) {
        float2 past = max(a - mitre, 0.0);
        float pastLength = length(past);
        return pastLength > 0.0 ? pastLength + mitre : max(a.x, a.y);
    }

    a += radius;
    float2 outside = max(a, 0.0);
    float inside = min(max(a.x, a.y), 0.0);

    if (exponent > 2.001 && outside.x + outside.y > 0.0) {
        float2 slope;
        float norm = StyledSuperellipse(outside, exponent, slope);
        return (norm + inside - radius) / max(length(slope), 0.25);
    }

    return length(outside) + inside - radius;
}

// Which way the shape's edge faces at a point: the direction the distance grows. Around a corner it turns
// with the curve. Deeper than a corner, where two sides are equally near, it fans smoothly from one side
// to the other instead of flipping on the diagonal.
float2 StyledShapeNormal(float2 p, float2 halfSize, float4 radii, float4 exponents)
{
    float radius;
    float exponent;
    float2 mirror;
    float2 a = StyledCornerSpace(p, halfSize, radii, exponents, radius, exponent, mirror) + max(radius, 0.0);

    float2 outside = max(a, 0.0);
    float2 normal;

    if (outside.x + outside.y <= 0.0) {
        normal = normalize(max(-a.yx, 1e-4));
    } else if (exponent > 2.001 && radius > 0.0) {
        float2 slope;
        StyledSuperellipse(outside, exponent, slope);
        normal = normalize(slope);
    } else {
        normal = outside / length(outside);
    }

    return normal * mirror;
}

// Where a point sits in a layer's gradient, 0 at Color and 1 at Color 2, before the range is applied.
// `scaledDirection` is the linear gradient's direction, scaled so it spans -0.5 to 0.5 across the shape.
// `edgePosition` is the Edge gradient's position, which the caller measures against the distance field.
float StyledGradientPosition(float gradientType, float2 q, float2 halfSize, float2 scaledDirection, float edgePosition)
{
    if (gradientType < STYLED_GRADIENT_LINEAR - 0.5) {
        return 0.0;
    }

    if (gradientType < STYLED_GRADIENT_RADIAL - 0.5) {
        return saturate(0.5 + dot(q, scaledDirection));
    }

    if (gradientType < STYLED_GRADIENT_EDGE - 0.5) {
        // An ellipse that reaches Color 2 at the rect's corners, so no part of the shape is left flat.
        return saturate(length(q / max(halfSize, 1e-4)) * 0.70710678);
    }

    return saturate(edgePosition);
}

#ifdef STYLED_EXTENDED

float StyledWrap(float x, float m)
{
    return x - m * floor(x / m);
}

// Distance along the shape's edge, clockwise from the middle of the top side, plus the edge's total length.
// Measured along the line `centerline` units from the shape's edge (negative inside), so corners get that
// line's length: wider arcs outside the shape, tighter inside.
// Where a corner's radius is smaller than the line's depth, the line has a sharp corner and the two sides
// meet on the diagonal like a mitre.
// Corners are measured by angle, which is exact for circular corners and close for smoothed ones.
float StyledEdgePosition(float2 p, float2 h, float4 radii, float4 exponents, float centerline, out float perimeter)
{
    // Per corner: how far the centerline's straight sides stop short of the shape's corner, and the radius
    // of the centerline's corner arc (0 for a mitred corner). A square corner stays square outside the
    // shape too: the line's sides run on past the corner and meet on the diagonal.
    float4 square = step(radii, 0.0);
    float4 inset = lerp(max(radii, -centerline), -centerline, square);
    float4 curve = lerp(max(radii + centerline, 0.0), 0.0, square);

    // A quarter circle is 1.5708 radii long, a quarter squircle 1.7544.
    float4 arcs = curve * lerp(1.5707963, 1.7544, saturate((exponents - 2.0) * 0.5));

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

    float radius;
    float exponent;
    float2 mirror;
    StyledCornerSpace(p, h, radii, exponents, radius, exponent, mirror);
    bool right = mirror.x > 0.0;
    bool top = mirror.y > 0.0;

    float cornerInset = top ? (right ? inset.y : inset.x) : (right ? inset.z : inset.w);
    float cornerCurve = top ? (right ? curve.y : curve.x) : (right ? curve.z : curve.w);
    float2 a = p * mirror - h;
    float2 d = a + cornerInset; // relative to the center of the centerline's corner

    bool onArc = cornerCurve > 0.0 && d.x > 0.0 && d.y > 0.0;

    // Mitred corners split their two sides on the diagonal through the corner.
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
    return StyledWrap(s, perimeter);
}

float StyledEdgeHash(float i, float seed)
{
    return frac(sin(i * 127.1 + seed * 311.7) * 43758.5453);
}

// A whole-number index wrapped into 0 to count - 1. The half step keeps an index that sits exactly on a
// multiple of the count from landing on the wrong side when the division is slightly off, and the result
// is rounded, because the hash turns the smallest error in its input into a different value. Without
// both, the noise shows a seam where the edge position wraps.
float StyledWrapIndex(float index, float count)
{
    return round(index - count * floor((index + 0.5) / count));
}

// Smooth value noise, -1 to 1, that repeats every `cells`, so it loops seamlessly around the edge.
float StyledLoopNoise(float x, float cells, float seed)
{
    float i = floor(x);
    float f = frac(x);
    float h0 = StyledEdgeHash(StyledWrapIndex(i, cells), seed);
    float h1 = StyledEdgeHash(StyledWrapIndex(i + 1.0, cells), seed);
    return lerp(h0, h1, f * f * (3.0 - 2.0 * f)) * 2.0 - 1.0;
}

// Soft lobes along the edge, -1 to 1: two octaves of looping noise. `lobeLength` is snapped so a whole
// number of lobes fits around the edge.
float StyledLobes(float along, float perimeter, float lobeLength, float seed)
{
    float cells = max(1.0, floor(perimeter / max(lobeLength, 1.0) + 0.5));
    float x = along / perimeter * cells;
    return (StyledLoopNoise(x, cells, seed) + 0.5 * StyledLoopNoise(x * 2.0, cells * 2.0, seed + 8.0)) / 1.5;
}

// How far the shape's edge is pushed out (positive) or pulled in at a point by the shape's wobble.
// wobble: amount, lobe length, seed, changes per second. With a speed the lobes morph in place: the edge
// blends from one set of lobes to the next and never travels around the shape.
float StyledShapeWobble(float2 p, float2 halfSize, float4 radii, float4 exponents, float4 wobble)
{
    if (wobble.x <= 0.0) {
        return 0.0;
    }

    float perimeter;
    float along = StyledEdgePosition(p, halfSize, radii, exponents, 0.0, perimeter);

    float time = _Time.y * wobble.w;
    float step = StyledWrap(floor(time), 64.0);
    float blend = frac(time);
    blend = blend * blend * (3.0 - 2.0 * blend);

    float lobes = StyledLobes(along, perimeter, wobble.y, wobble.z + step * 3.0);

    if (wobble.w != 0.0) {
        float next = StyledLobes(along, perimeter, wobble.y, wobble.z + StyledWrap(step + 1.0, 64.0) * 3.0);
        lobes = lerp(lobes, next, blend);
    }

    return wobble.x * lobes;
}

// Coverage of a dashed stroke. `along` is the position along the edge and `perimeter` its length.
// stroke: spacing, dash length as a fraction of the spacing plus 2 for round caps, speed.
// The spacing snaps so a whole number of dashes fits around the edge.
float StyledDashCoverage(float sd, float along, float perimeter, float bandStart, float bandEnd, float pixel, float4 stroke)
{
    float spacing = max(stroke.x, 1.0);
    bool roundCaps = stroke.y >= 2.0;
    float dashFraction = saturate(stroke.y - (roundCaps ? 2.0 : 0.0));
    float thickness = max(bandEnd - bandStart, 1e-3);

    float period = perimeter / max(1.0, floor(perimeter / spacing + 0.5));
    float phase = period * frac(stroke.z * _Time.y / period);
    float halfDash = dashFraction * period * 0.5;

    // Distance along the edge to the nearest dash center.
    float toCenter = abs(frac((along - phase - halfDash) / period + 0.5) - 0.5) * period;

    if (roundCaps) {
        // Each dash is a capsule, with the caps inside the dash length. Zero length gives dots.
        float halfThickness = thickness * 0.5;
        float gap = max(toCenter - max(halfDash - halfThickness, 0.0), 0.0);
        return 1.0 - StyledRamp(length(float2(gap, sd - (bandStart + bandEnd) * 0.5)) - halfThickness, pixel, pixel);
    }

    float band = (1.0 - StyledRamp(sd - bandEnd, pixel, pixel)) * StyledRamp(sd - bandStart, pixel, pixel);
    return band * StyledRamp(halfDash - toCenter, pixel, pixel);
}

#endif

#endif
