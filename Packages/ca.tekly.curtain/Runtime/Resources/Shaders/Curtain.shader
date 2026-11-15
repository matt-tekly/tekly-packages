// Curtain: a full-screen transition in one pass. Every pixel gets a field value from 0 to 1 for the
// curtain's shape, and each of up to four colour bands covers the pixels whose field is below that band's
// progress. A later band replaces what the bands before it drew, so a band can also be clear, and what
// is behind shows through where no band covers. With CURTAIN_CONTENT the top band draws a texture instead
// of a colour: the CurtainStage's live render of a shared curtain's content canvas.
//
// CurtainGraphic works out every band's progress, the shape and the field's direction on the CPU.
// The shader only reads uniforms, so a tween changes material values and never rebuilds the canvas.
Shader "UI/Curtain"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Content ("Content", 2D) = "black" {}
        _Gradient ("Gradient", 2D) = "black" {}

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
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
        // Premultiplied alpha, which is also what the UI shaders leave in a canvas render.
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // 3.5 matches the Styled Rect shaders: WebGL 2 and GLES 3 and up.
            #pragma target 3.5

            // The material is made at runtime, so these must be multi_compile: shader_feature would be
            // stripped from builds. Diagonal is the shape with no keyword.
            #pragma multi_compile_local _ CURTAIN_IRIS CURTAIN_GRADIENT
            #pragma multi_compile_local _ CURTAIN_CONTENT

            #include "UnityCG.cginc"

            sampler2D _Content;
            sampler2D _Gradient;

            half4 _Color0;
            half4 _Color1;
            half4 _Color2;
            half4 _Color3;

            // Each band's progress, from 0 (none of the field covered) to 1 (all of it). An unused band is
            // far below 0 so it never covers anything.
            float4 _Progress;
            // x = field scale, y = field offset. Flips the field for Invert and for a curtain that is leaving.
            float4 _Field;
            // Diagonal: xy = direction divided by the rect's extent along it, z = 1 for a split.
            // Iris: xy = center, z = 1 / the farthest corner's distance, w = 1 for a diamond.
            float4 _Shape;
            // xy = scales a position into the gradient's uv, z = the visible ramp's least value,
            // w = 1 / the visible ramp's range.
            float4 _GradientRange;
            float _Softness;
            float _UIVertexColorAlwaysGammaSpace;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv0        : TEXCOORD0; // position from the rect's center, in rect heights
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color       : COLOR;
                float2 local      : TEXCOORD0;
#ifdef CURTAIN_CONTENT
                float4 screen     : TEXCOORD1;
#endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = UnityObjectToClipPos(IN.positionOS);
                OUT.local = IN.uv0;

                // The layer colors are linear by now in a linear project, so a vertex color the canvas
                // keeps in gamma is brought to linear too.
                float4 color = IN.color;
                if (_UIVertexColorAlwaysGammaSpace > 0.5 && !IsGammaSpace()) {
                    color.rgb = GammaToLinearSpace(color.rgb);
                }

                OUT.color = color;

#ifdef CURTAIN_CONTENT
                OUT.screen = ComputeScreenPos(OUT.positionCS);
#endif
                return OUT;
            }

            // The shape's field at a position: 0 where the curtain starts covering, 1 where it finishes.
            float CurtainField(float2 p)
            {
#if defined(CURTAIN_IRIS)
                float2 d = abs(p - _Shape.xy);
                return lerp(length(d), d.x + d.y, _Shape.w) * _Shape.z;
#elif defined(CURTAIN_GRADIENT)
                float ramp = tex2D(_Gradient, p * _GradientRange.xy + 0.5).r;
                return (ramp - _GradientRange.z) * _GradientRange.w;
#else
                float d = dot(p, _Shape.xy); // -1 to 1 across the rect
                return lerp(d * 0.5 + 0.5, abs(d), _Shape.z);
#endif
            }

            half4 Band(half4 color, half4 tint)
            {
                color *= tint;
                color.rgb *= color.a;
                return color;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float field = saturate(CurtainField(IN.local)) * _Field.x + _Field.y;

                // The edge is at least a pixel wide so it is antialiased. Progress is stretched by that
                // width so 0 still leaves every pixel open and 1 still covers every pixel.
                float width = max(fwidth(field), _Softness) + 1e-5;
                float4 progress = _Progress * (1.0 + width) - 0.5 * width;
                float4 cover = saturate((progress - field) / width + 0.5);

#ifdef CURTAIN_CONTENT
                // The canvas render is already premultiplied. The tint's alpha fades it with the bands.
                half4 top = tex2D(_Content, IN.screen.xy / IN.screen.w) * IN.color.a;
#else
                half4 top = Band(_Color3, IN.color);
#endif

                // A band replaces what is under it, so a clear band opens a hole.
                half4 color = half4(0, 0, 0, 0);
                color = lerp(color, Band(_Color0, IN.color), cover.x);
                color = lerp(color, Band(_Color1, IN.color), cover.y);
                color = lerp(color, Band(_Color2, IN.color), cover.z);
                color = lerp(color, top, cover.w);
                return color;
            }
            ENDCG
        }
    }
}
