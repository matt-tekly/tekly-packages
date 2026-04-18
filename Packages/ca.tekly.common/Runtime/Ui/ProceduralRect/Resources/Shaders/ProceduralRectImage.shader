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
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

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
                float2 uv0        : TEXCOORD0;
                float2 rectSize   : TEXCOORD1;
                float2 packedData : TEXCOORD2;
                float2 shapeData  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                fixed4 color         : COLOR;
                float4 localPosition : TEXCOORD0;
                float4 cornerRadii   : TEXCOORD1;
                float2 uv            : TEXCOORD2;
                float2 rectSize      : TEXCOORD3;
                float  lineWeight    : TEXCOORD4;
                float  aaScale       : TEXCOORD5;
                float  falloffPower  : TEXCOORD6;
                float4 mask          : TEXCOORD7;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 DecodePacked01x2(float packedValue)
            {
                const float2 decodeMul = float2(1.0, 65535.0);
                const float decodeBit = 1.0 / 65535.0;

                float2 unpacked = frac(decodeMul * packedValue);
                unpacked.x -= unpacked.y * decodeBit;
                return unpacked;
            }

            half RoundedRectInsideDistance(float2 pixelPos, float4 cornerRadii, float2 rectSize)
            {
                half4 edgeDistances = half4(
                    pixelPos.x,
                    pixelPos.y,
                    rectSize.x - pixelPos.x,
                    rectSize.y - pixelPos.y
                );

                bool4 inCornerRegion = bool4(
                    all(edgeDistances.xw < cornerRadii.x),
                    all(edgeDistances.zw < cornerRadii.y),
                    all(edgeDistances.zy < cornerRadii.z),
                    all(edgeDistances.xy < cornerRadii.w)
                );

                half nearestStraightEdge = min(min(edgeDistances.x, edgeDistances.y), min(edgeDistances.z, edgeDistances.w));

                half4 cornerDistances = cornerRadii - half4(
                    length(edgeDistances.xw - cornerRadii.x),
                    length(edgeDistances.zw - cornerRadii.y),
                    length(edgeDistances.zy - cornerRadii.z),
                    length(edgeDistances.xy - cornerRadii.w)
                );

                half4 distanceByCorner = min(inCornerRegion * max(cornerDistances, 0.0), nearestStraightEdge)
                    + (1 - inCornerRegion) * nearestStraightEdge;

                half nearestCornerDistance = min(min(distanceByCorner.x, distanceByCorner.y), min(distanceByCorner.z, distanceByCorner.w));
                return any(inCornerRegion) ? nearestCornerDistance : nearestStraightEdge;
            }

            float ComputeCoverage(float insideDistance, float lineWeight, float aaScale, float falloffPower)
            {
                float rampCenter = (lineWeight + 1.0 / aaScale) * 0.5;
                float coverage = saturate((rampCenter - abs(insideDistance - rampCenter)) * aaScale);
                return pow(coverage, falloffPower);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.localPosition = IN.positionOS;
                OUT.positionCS = UnityObjectToClipPos(IN.positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv0, _MainTex);
                OUT.rectSize = IN.rectSize;

                float minSide = min(IN.rectSize.x, IN.rectSize.y);
                float2 topPair = DecodePacked01x2(IN.packedData.x);
                float2 bottomPair = DecodePacked01x2(IN.packedData.y);
                float2 shapeParams = DecodePacked01x2(IN.shapeData.x);

                OUT.cornerRadii = float4(topPair.x, topPair.y, bottomPair.x, bottomPair.y) * minSide;
                OUT.lineWeight = shapeParams.x * minSide * 0.5;
                OUT.aaScale = clamp(IN.shapeData.y, 1.0 / 2048.0, 2048.0);
                OUT.falloffPower = lerp(0.25, 4.0, shapeParams.y);
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
                half4 color = (tex2D(_MainTex, IN.uv) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                half2 maskFactor = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= maskFactor.x * maskFactor.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                half insideDistance = RoundedRectInsideDistance(IN.uv * IN.rectSize, IN.cornerRadii, IN.rectSize);
                color.a *= ComputeCoverage(insideDistance, IN.lineWeight, IN.aaScale, IN.falloffPower);

                if (color.a <= 0)
                {
                    discard;
                }

                return color;
            }
            ENDCG
        }
    }
}
