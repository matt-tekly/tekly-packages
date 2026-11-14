// Styled Rect, Core tier. Draws one layer of a StyledRect per quad against the rect's distance field.
// Every value comes from the StyledRectStore's data texture, fetched in the vertex shader and passed on as
// flat varyings, so the fragment shader only does the per-pixel work.
Shader "UI/Styled Rect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // Set by StyledRectStore for each blend mode. The defaults are Normal (premultiplied alpha).
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        // 0 = premultiplied color, 1 = color faded toward white (Multiply)
        _BlendColorOutput ("Blend Color Output", Float) = 0
        // 0 = coverage alpha, 1 = always 0, 2 = always 1 (both leave the alpha behind untouched)
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
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // 3.5 for Load (texelFetch) and flat varyings. WebGL2 / GLES3 and up.
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "StyledRectCore.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            float _BlendColorOutput;
            float _BlendAlphaOutput;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

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
                nointerpolation float4 soft       : TEXCOORD7; // xy = shadow offset, z = soft width, w = shadow spread
                nointerpolation float4 softRadii  : TEXCOORD8; // the radii an inner shadow or Edge gradient measures against
                // x = layer kind, y = 1 when a shadow shows behind the shape, plus 2 when the layer is textured,
                // plus 4 when it is an underlay. zw = gradient range start, end
                nointerpolation float4 kind       : TEXCOORD9;
                nointerpolation float4 cornerExponents : TEXCOORD10; // per corner: 2 is circular, 4 a squircle
                nointerpolation float4 uvLimits   : TEXCOORD11; // min x, min y, max x, max y the texture uv may reach
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
                float4 header = StyledLoad(layer + STYLED_LAYER_HEADER); // kind, flags, band start, band end
                float4 gradient = StyledLoad(layer + STYLED_LAYER_GRADIENT); // type, angle, range start, range end
                float layerKind = round(header.x);

                OUT.shape = float4(halfSize, header.zw);
                OUT.radii = StyledLoad(shape + STYLED_SHAPE_RADII);
                OUT.cornerExponents = StyledLoad(shape + STYLED_SHAPE_CORNER_EXPONENTS);
                // Flags: 1 = underlay, 2 = show behind, 4 = textured, 8 = ignore tint.
                float flags = round(header.y);
                float ignoreTint = floor(flags / 8.0);
                flags -= ignoreTint * 8.0;

                // The vertex color is the rect's tint times whatever colors the whole graphic. A layer that
                // ignores the tint keeps its own colors but still fades with the tint's alpha.
                float4 tint = float4(ignoreTint > 0.5 ? float3(1, 1, 1) : IN.color.rgb, IN.color.a);

                OUT.colorA = StyledPrepareColor(StyledLoad(layer + STYLED_LAYER_COLOR_A), tint);
                OUT.colorB = StyledPrepareColor(StyledLoad(layer + STYLED_LAYER_COLOR_B), tint);

                // Angles follow CSS: 0 points up and they turn clockwise.
                float2 direction = float2(sin(gradient.y), cos(gradient.y));
                float extent = abs(direction.x) * halfSize.x + abs(direction.y) * halfSize.y;
                float2 scaledDirection = direction / max(2.0 * extent, 1e-4);

                float textured = floor(flags / 4.0);
                flags -= textured * 4.0;
                float showBehind = floor(flags / 2.0);
                float underlay = flags - showBehind * 2.0;

                // A row that never had an exponent written reads as 0, which means a straight gradient.
                float exponent = StyledLoad(layer + STYLED_LAYER_GRADIENT_CURVE).x;
                exponent = exponent > 0.0 ? exponent : 1.0;

                OUT.gradient = float4(layerKind > STYLED_KIND_BEVEL - 0.5 ? direction : scaledDirection, round(gradient.x), exponent);
                OUT.soft = StyledLoad(layer + STYLED_LAYER_SHADOW);
                OUT.softRadii = StyledLoad(layer + STYLED_LAYER_SOFT_RADII);
                OUT.kind = float4(layerKind, showBehind + textured * 2.0 + underlay * 4.0, gradient.zw);

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

            fixed4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.local.xy;
                float2 halfSize = IN.shape.xy;

                // Taken outside the branch that samples the texture, where screen derivatives are not valid.
                float2 uv = IN.local.zw;
                float2 uvPerPixelX = ddx(uv);
                float2 uvPerPixelY = ddy(uv);
                float underlay = floor(IN.kind.y / 4.0);
                float textured = floor((IN.kind.y - underlay * 4.0) / 2.0);
                float showBehind = IN.kind.y - underlay * 4.0 - textured * 2.0;
                float bandStart = IN.shape.z;
                float bandEnd = IN.shape.w;
                float kind = IN.kind.x;
                float gradientType = IN.gradient.z;

                float pixel = StyledPixelSize(p);
                float softWidth = max(IN.soft.z, pixel);

                // How much of the pixel the layer covers, and where it sits between Color (0) and Color 2 (1).
                float coverage;
                float t;

                if (kind < STYLED_KIND_SHADOW - 0.5) {
                    // Fill or stroke: a band of the distance field.
                    // A stroke keeps square corners square out to its outer edge.
                    float mitre = max(bandEnd, 0.0);

                    // A blurred fill measures against the soft radii, so its soft edge does not crease into the
                    // corners. Strokes have no soft edge.
                    bool softFill = kind < STYLED_KIND_STROKE - 0.5 && IN.soft.z > 0.0;
                    float sd = StyledShapeDistance(p, halfSize, softFill ? IN.softRadii : IN.radii, IN.cornerExponents, mitre);

                    // An underlay stops a pixel short of its edge: an opaque stroke above ends there too, and two
                    // anti-aliased edges in the same place would let this layer's color show through as a fringe.
                    float outer = 1.0 - StyledRamp(sd - bandEnd + underlay * pixel, softWidth, pixel);
                    float inner = bandStart < STYLED_NO_INNER_EDGE ? 1.0 : saturate(0.5 + (sd - bandStart) / pixel);
                    coverage = outer * inner;

                    float edgePosition = 0.0;

                    if (gradientType > STYLED_GRADIENT_EDGE - 0.5) {
                        // 0 at the layer's outer edge, 1 at its inner edge (or the deepest point of a fill).
                        // Measured against the soft radii, so the gradient does not crease into the corners.
                        float depth = bandStart < STYLED_NO_INNER_EDGE ? min(halfSize.x, halfSize.y) + bandEnd : bandEnd - bandStart;
                        float softDistance = StyledShapeDistance(p, halfSize, IN.softRadii, IN.cornerExponents, mitre);
                        edgePosition = (bandEnd - softDistance) / max(depth, 1e-4);
                    }

                    t = ApplyRange(StyledGradientPosition(gradientType, p, halfSize, IN.gradient.xy, edgePosition), IN.kind.zw, IN.gradient.w);
                } else if (kind < STYLED_KIND_BEVEL - 0.5) {
                    // Shadows carry their gradient along with their offset. An Edge gradient has no meaning
                    // on a shadow and draws as Color.
                    float2 q = p - IN.soft.xy;
                    float spread = IN.soft.w;
                    float shapeDistance = StyledShapeDistance(p, halfSize, IN.radii, IN.cornerExponents, 0.0);

                    if (kind < STYLED_KIND_INNER_SHADOW - 0.5) {
                        // Drop shadow: the shape moved by the offset and grown by the spread, with a soft edge.
                        // Square corners stay square out to the spread.
                        float sd = StyledShapeDistance(q, halfSize, IN.radii, IN.cornerExponents, max(spread, 0.0));
                        coverage = 1.0 - StyledRamp(sd - spread, softWidth, pixel);

                        // Cut out under the shape, as CSS does. The cut sits on the inner half of the shape's
                        // anti-aliased edge, so the shadow still backs that edge and no light fringe appears.
                        coverage *= max(saturate(shapeDistance / pixel + 1.0), showBehind);
                    } else {
                        // Inner shadow: inside the real shape, shadowed where the offset shape, pushed in by
                        // the spread, is not. That shape uses the soft radii so its corners do not crease.
                        float sd = StyledShapeDistance(q, halfSize, IN.softRadii, IN.cornerExponents, 0.0);
                        coverage = saturate(0.5 - shapeDistance / pixel) * StyledRamp(sd + spread, softWidth, pixel);
                    }

                    t = ApplyRange(StyledGradientPosition(gradientType, q, halfSize, IN.gradient.xy, 0.0), IN.kind.zw, IN.gradient.w);
                } else {
                    // Bevel: a band just inside the edge, lit by which way the edge faces. Color is the
                    // highlight (facing the light) and Color 2 the shadow (facing away).
                    float sd = StyledShapeDistance(p, halfSize, IN.radii, IN.cornerExponents, 0.0);
                    float facing = dot(StyledShapeNormal(p, halfSize, IN.radii, IN.cornerExponents), IN.gradient.xy);

                    coverage = saturate(0.5 - sd / pixel) * StyledRamp(sd - bandStart, softWidth, pixel) * saturate(abs(facing) * 1.4142136);
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

                // Shape each blend mode's output so that transparent pixels leave what is behind unchanged.
                // Faded toward white is lerp(1, straight color, alpha), written here for a premultiplied color.
                float3 rgb = _BlendColorOutput > 0.5 ? 1.0 - color.a + color.rgb : color.rgb;
                float alpha = _BlendAlphaOutput > 1.5 ? 1.0 : (_BlendAlphaOutput > 0.5 ? 0.0 : color.a);

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
