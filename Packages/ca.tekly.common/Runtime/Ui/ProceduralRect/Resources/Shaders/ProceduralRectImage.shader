Shader "UI/Procedural Rect Image"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // 3.0 for fwidth (screen-space anti-aliasing). Fine for WebGL2 / GLES3 and up.
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _MainTex_ST;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float4 uv0        : TEXCOORD0; // xy = texture uv, zw = normalized shape coordinate
                float2 rectSize   : TEXCOORD1;
                float2 packedData : TEXCOORD2;
                float2 shapeData  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                fixed4 color       : COLOR;
                float4 uv          : TEXCOORD0;
                float4 cornerRadii : TEXCOORD1;
                float2 rectSize    : TEXCOORD2;
                float3 shapeParams : TEXCOORD3; // x = line weight, y = falloff ramp width, z = falloff power
                float4 mask        : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Inverse of ProceduralRectImage.Pack: two 12-bit values stored as an integer below 2^24.
            float2 UnpackTwo12(float packedValue)
            {
                float n = round(packedValue * 16777216.0);
                float high = floor(n / 4096.0);
                float low = n - high * 4096.0;
                return float2(high, low) / 4095.0;
            }

            // Distance from the shape edge, positive inside. Radii are TL, TR, BR, BL.
            // Kept in full float: values are in pixels and exceed half precision on large rects.
            float RoundedRectInsideDistance(float2 pixelPos, float4 cornerRadii, float2 rectSize)
            {
                // Distance to each edge: left, bottom, right, top.
                float4 edges = float4(pixelPos.x, pixelPos.y, rectSize.x - pixelPos.x, rectSize.y - pixelPos.y);
                float straight = min(min(edges.x, edges.y), min(edges.z, edges.w));

                // Per corner (TL, TR, BR, BL): offset from the corner circle's center.
                float4 offsetX = cornerRadii - float4(edges.x, edges.z, edges.z, edges.x);
                float4 offsetY = cornerRadii - float4(edges.w, edges.w, edges.y, edges.y);
                float4 inCorner = step(0.0, offsetX) * step(0.0, offsetY);
                float4 arc = cornerRadii - sqrt(offsetX * offsetX + offsetY * offsetY);

                float4 perCorner = lerp(straight.xxxx, min(arc, straight.xxxx), inCorner);
                return min(min(perCorner.x, perCorner.y), min(perCorner.z, perCorner.w));
            }

            float ComputeCoverage(float insideDistance, float lineWeight, float rampWidth, float falloffPower)
            {
                float rampCenter = (lineWeight + rampWidth) * 0.5;
                float coverage = saturate((rampCenter - abs(insideDistance - rampCenter)) / rampWidth);
                return pow(coverage, falloffPower);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = UnityObjectToClipPos(IN.positionOS);

                OUT.uv.xy = TRANSFORM_TEX(IN.uv0.xy, _MainTex);
                OUT.uv.zw = IN.uv0.zw;

                OUT.rectSize = IN.rectSize;

                float minSide = min(IN.rectSize.x, IN.rectSize.y);
                float2 topPair = UnpackTwo12(IN.packedData.x);
                float2 bottomPair = UnpackTwo12(IN.packedData.y);
                float2 shapeParams = UnpackTwo12(IN.shapeData.x);

                OUT.cornerRadii = float4(topPair.x, topPair.y, bottomPair.x, bottomPair.y) * minSide;
                OUT.shapeParams = float3(
                    shapeParams.x * minSide * 0.5,
                    1.0 / clamp(IN.shapeData.y, 1.0 / 2048.0, 2048.0),
                    lerp(0.25, 4.0, shapeParams.y)
                );
                OUT.color = IN.color * _Color;

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
                half4 color = (tex2D(_MainTex, IN.uv.xy) + _TextureSampleAdd) * IN.color;

                float2 pixelPos = IN.uv.zw * IN.rectSize;

                // Never let the edge ramp get narrower than one screen pixel, whatever the canvas scale.
                float screenPixelInLocal = length(fwidth(pixelPos)) * 0.70710678;
                float rampWidth = max(IN.shapeParams.y, screenPixelInLocal);

                float insideDistance = RoundedRectInsideDistance(pixelPos, IN.cornerRadii, IN.rectSize);
                color.a *= ComputeCoverage(insideDistance, IN.shapeParams.x, rampWidth, IN.shapeParams.z);

                #ifdef UNITY_UI_CLIP_RECT
                half2 maskFactor = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= maskFactor.x * maskFactor.y;
                #endif

                // Mask's stencil material enables this keyword, so the rounded shape still clips children.
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
