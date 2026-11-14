// Styled Rect, Extended tier. The Core shader plus what needs the position along the shape's edge: dashed
// strokes, strokes whose width wobbles, and a shape whose whole edge wobbles. StyledRect picks this shader
// only for rects that use one of those. The programs are in StyledRectPass.cginc.
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

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #define STYLED_EXTENDED
            #include "StyledRectPass.cginc"
            ENDCG
        }
    }
}
