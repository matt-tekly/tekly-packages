// Vignette. Darkens a rect toward its corners with the graphic's color, the alpha setting how strong it is.
// The shape is a superellipse fitted to the rect, the shape post processing vignettes use: every curve of
// the fade is the same shape at a smaller size, so it never creases. Every value comes in the vertices, so
// vignettes with the same blend mode share one material and batch together.
// uv0: xy = position from the rect's center, 1 at its sides.
// uv1: x = superellipse exponent, y = radius where the fade starts, z = radius of the corners, where it ends,
//      w = the bias exponent.
Shader "UI/Vignette"
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

        // Set by Vignette for each blend mode. The defaults are Normal (premultiplied alpha).
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        // 0 = premultiplied color over what is behind, 1 = a color to multiply what is behind by
        _Multiply ("Multiply", Float) = 0
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
        Blend [_SrcBlend] [_DstBlend]
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            float4 _ClipRect;
            float _UIVertexColorAlwaysGammaSpace;
            float _Multiply;

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float4 uv0    : TEXCOORD0;
                float4 uv1    : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex   : SV_POSITION;
                half4 color     : COLOR;
                float2 local    : TEXCOORD0;
                float4 shape    : TEXCOORD1;
                float4 world    : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.world = IN.vertex;
                OUT.local = IN.uv0.xy;
                OUT.shape = IN.uv1;

                float3 rgb = IN.color.rgb;

                if (_UIVertexColorAlwaysGammaSpace > 0.5 && !IsGammaSpace()) {
                    rgb = GammaToLinearSpace(rgb);
                }

                OUT.color = half4(rgb, IN.color.a);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float exponent = IN.shape.x;
                float start = IN.shape.y;
                float end = IN.shape.z;

                // The superellipse radius: 1 at the middle of each side, more toward the corners.
                float2 u = max(abs(IN.local), 1e-5);
                float radius = pow(pow(u.x, exponent) + pow(u.y, exponent), 1.0 / exponent);

                // 1 where the vignette is clear, 0 at the corners. Bent by the bias, then eased at both ends so
                // neither the clear edge nor the corners show a line where the fade's slope jumps.
                float t = 1.0 - saturate((radius - start) / max(end - start, 1e-4));
                t = t > 0.0 ? pow(t, IN.shape.w) : 0.0;
                t = t * t * (3.0 - 2.0 * t);

                half alpha = IN.color.a * (1.0 - t);

                // Dither by a fraction of an 8 bit step so the long, dark fade does not band. Kept off where the
                // vignette is clear, so that stays exactly untouched.
                float noise = frac(52.9829189 * frac(dot(IN.vertex.xy, float2(0.06711056, 0.00583715))));
                alpha = alpha > 0.0 ? saturate(alpha + (noise - 0.5) / 255.0) : 0.0;

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(IN.world.xy, _ClipRect);
                #endif

                if (_Multiply > 0.5) {
                    // Multiplies what is behind: white where clear, the color at full strength. Alpha is kept.
                    return half4(lerp(half3(1, 1, 1), IN.color.rgb, alpha), 1.0);
                }

                return half4(IN.color.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}
