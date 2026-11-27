// Styled Rect, Extended tier. The Core shader plus dashed strokes. StyledRect picks this shader only for
// rects that have one. The programs are in StyledRectPass.cginc.
Shader "UI/Styled Rect Extended"
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

        // Set by StyledRectStore for each blend mode, along with an output keyword. The defaults are
        // Normal (premultiplied alpha).
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
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

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            // The blend mode's output: none is Normal, ADD is Additive and Screen, MULTIPLY is Multiply.
            // multi_compile, because the materials are made at runtime and a shader_feature would be stripped.
            #pragma multi_compile_local _ STYLED_OUTPUT_ADD STYLED_OUTPUT_MULTIPLY

            #define STYLED_EXTENDED
            #include "StyledRectPass.cginc"
            ENDCG
        }
    }
}
