// Wipe: a screen transition in one pass. Every pixel gets a field value from 0 to 1, where 0 is covered
// first and 1 last. A layer covers the pixels whose field is between its tail and its head, so raising the
// heads covers the rect and raising the tails uncovers it again the same way. Up to three colour layers
// lead the fill, and a later layer replaces what the ones before it drew.
//
// The field comes from three parts, like a motion design tool's duplicator:
// - A base field: linear, radial, angular or a ramp texture.
// - An optional repeat into lanes or a grid. Each cell samples the base field at its center for its delay
//   and adds its own element, a shape that grows or a bar that slides.
// - Edge modulation, a zig zag or wave pushed into the field.
//
// Two fields are not thresholds. WIPE_SHAPE scales a silhouette's distance texture about a point, and
// WIPE_FADE uses the fill's head as an alpha.
//
// Wipe.cs works out every head, tail and field value on the CPU. The shader only reads uniforms, so a
// tween changes material values and never rebuilds the canvas.
Shader "UI/Tekly/Wipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Fill ("Fill", 2D) = "white" {}
        _Ramp ("Ramp", 2D) = "black" {}
        _ShapeTex ("Shape", 2D) = "black" {}

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
            // WebGL 2 and GLES 3 and up.
            #pragma target 3.5

            // The material is made at runtime, so these must be multi_compile: shader_feature would be
            // stripped from builds. Linear is the field with no keyword.
            #pragma multi_compile_local _ WIPE_RADIAL WIPE_ANGULAR WIPE_RAMP WIPE_SHAPE WIPE_FADE
            #pragma multi_compile_local _ WIPE_LANES WIPE_GRID
            #pragma multi_compile_local _ WIPE_FILL_TEXTURE

            #include "UnityCG.cginc"

            // A silhouette and a fade have no field for a cell to sample.
            #if defined(WIPE_SHAPE) || defined(WIPE_FADE)
                #undef WIPE_LANES
                #undef WIPE_GRID
            #endif

            sampler2D _Fill;
            sampler2D _Ramp;
            sampler2D _ShapeTex;

            // Straight alpha. Premultiplied in the shader.
            half4 _Color0;
            half4 _Color1;
            half4 _Color2;
            half4 _ColorFill;
            // Drawn where nothing covers. Clear, except in the editor preview of a live reveal.
            half4 _Backdrop;

            // Each layer's head and tail from 0 to 1, the fill in w. An unused layer's head is far below 0.
            float4 _Heads;
            float4 _Tails;

            // Linear: xy = direction divided by the rect's extent along it, z = 1 to mirror.
            // Radial: xy = center, z = 1 / the metric at the farthest corner, w = 1 for a circle.
            // Angular: xy = center, z = arms, w = twist.
            // Ramp: xy = scales a position into the ramp's uv, z = the least value on screen,
            // w = 1 / the range of values on screen.
            // Shape: xy = center, z = the scale at which the silhouette's hole clears the rect.
            float4 _FieldA;
            // Linear: xy = the unit direction along the edge.
            // Radial: x = half a star sector in radians, y = the star's slope, z = rotation in radians.
            // Angular: x = 1 / the farthest corner's distance, y = 1 clockwise or -1, z = start in turns.
            // Shape: x = the distance one texture step covers, y and z = cos and sin of the rotation.
            float4 _FieldB;
            // x and y = scale and offset that run the field backwards for Invert, z = softness,
            // w = 1 when a silhouette covers its inside instead of its outside.
            float4 _FieldOp;
            // x = amplitude as a fraction of the sweep, y = frequency, z = 0 zig zag or 1 sine.
            float4 _Wave;

            // Lanes: xy = the direction along the lanes, z = lane width, w = 1 / the rect's extent along them.
            // Grid: x = cell size, y and z = cos and sin of the grid's rotation.
            float4 _RepeatA;
            // x = spread, y = random, z = alternate.
            // w = the element. Lanes: 0 grow, 1 slide. Grid: 0 diamond, 1 circle, 2 square.
            float4 _RepeatB;
            // Lanes: x = cap (0 flat, 1 round, 2 chevron), y = the cap's length as a fraction of the sweep.
            float4 _RepeatC;

            // xy = the fill texture's uv offset, zw = its uv scale.
            float4 _FillRect;
            float _FillPremultiplied;
            // xy = scales a position in rect heights into the rect's 0 to 1 uv.
            float4 _RectScale;

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
                half alpha        : COLOR;
                float2 local      : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = UnityObjectToClipPos(IN.positionOS);
                OUT.local = IN.uv0;
                // Only the alpha is used, which carries a CanvasGroup's fade.
                OUT.alpha = IN.color.a;
                return OUT;
            }

            half4 Premultiply(half4 color)
            {
                color.rgb *= color.a;
                return color;
            }

            float Hash(float id)
            {
                return frac(sin(id * 12.9898) * 43758.5453);
            }

            // 0 to 1 and back over one unit.
            float Wave(float x)
            {
                float zigzag = abs(frac(x) * 2.0 - 1.0);
                float sine = cos(x * 6.2831853) * 0.5 + 0.5;
                return lerp(zigzag, sine, _Wave.z);
            }

            // The base field at a position, before Invert. x is the field, y runs along the edge for the
            // modulation. shift moves an angular field half an arm so its wrap is somewhere else.
            float2 BaseField(float2 p, float shift)
            {
            #if defined(WIPE_RADIAL)
                float2 d = p - _FieldA.xy;
                float r = length(d);
                float angle = atan2(d.x, d.y) + _FieldB.z;
                // The angle inside one star sector, 0 at a tip and _FieldB.x at a valley.
                float sector = abs(frac(angle / (2.0 * _FieldB.x) + 0.5) - 0.5) * 2.0 * _FieldB.x;
                float star = r * (cos(sector) + sin(sector) * _FieldB.y);
                float metric = lerp(star, r, _FieldA.w) * _FieldA.z;
                // The far parts go first, so the natural direction is an iris closing.
                return float2(1.0 - saturate(metric), angle * 0.15915494);
            #elif defined(WIPE_ANGULAR)
                float2 d = p - _FieldA.xy;
                float r = length(d);
                float turns = atan2(d.x, d.y) * 0.15915494 * _FieldB.y - _FieldB.z;
                return float2(frac(turns * _FieldA.z + r * _FieldB.x * _FieldA.w + shift), r);
            #elif defined(WIPE_RAMP)
                float ramp = tex2Dlod(_Ramp, float4(p * _FieldA.xy + 0.5, 0, 0)).r;
                return float2(saturate((ramp - _FieldA.z) * _FieldA.w), 0.0);
            #else
                float d = dot(p, _FieldA.xy); // -1 to 1 across the rect
                return float2(lerp(d * 0.5 + 0.5, abs(d), _FieldA.z), dot(p, _FieldB.xy));
            #endif
            }

            float Invert(float field)
            {
                return saturate(field) * _FieldOp.x + _FieldOp.y;
            }

            // The inverted and modulated field at a pixel.
            float PixelField(float2 p, float shift)
            {
                float2 base = BaseField(p, shift);
                float field = Invert(base.x);
                return (field + _Wave.x * Wave(base.y * _Wave.y)) / (1.0 + _Wave.x);
            }

            // When a cell starts, from the base field at its center mixed with a random value.
            float Delay(float2 center, float id)
            {
                float field = Invert(BaseField(center, 0.0).x);
                return lerp(field, Hash(id), _RepeatB.y);
            }

            #if defined(WIPE_LANES)
            // A lane's field at a position. across is the position across the lanes in lane widths.
            float LaneField(float2 p, float lane, float across)
            {
                float2 direction = _RepeatA.xy;
                float2 perpendicular = float2(-direction.y, direction.x);
                float odd = fmod(abs(lane), 2.0);
                float flip = odd * _RepeatB.z;

                float delay = Delay(perpendicular * (lane + 0.5) * _RepeatA.z, lane);
                float element;

                if (_RepeatB.w > 0.5) {
                    // A bar slides along the lane. The cap pushes the sides back, so the tip leads.
                    float along = dot(p, direction) * _RepeatA.w * 0.5 + 0.5;
                    along = lerp(along, 1.0 - along, flip);

                    float side = clamp((across - lane) * 2.0 - 1.0, -1.0, 1.0);
                    float rounded = 1.0 - sqrt(saturate(1.0 - side * side));
                    float cap = _RepeatC.x < 0.5 ? 0.0 : (_RepeatC.x < 1.5 ? rounded : abs(side));

                    element = (along + _RepeatC.y * cap) / (1.0 + _RepeatC.y);
                } else {
                    // A blind grows across the lane.
                    float grown = saturate(across - lane);
                    element = lerp(grown, 1.0 - grown, flip);
                }

                return delay * _RepeatB.x + element * (1.0 - _RepeatB.x);
            }
            #endif

            // Layers the colours and the fill over the backdrop for a field value. width is how much the
            // field changes across a pixel, so the edge is at least a pixel wide. Heads and tails are
            // stretched by the edge's width so 0 still leaves every pixel open and 1 covers every pixel.
            half4 Compose(float field, float width, half4 fill)
            {
                width = max(width, _FieldOp.z) + 1e-5;

                float4 heads = _Heads * (1.0 + width) - 0.5 * width;
                float4 tails = _Tails * (1.0 + width) - 0.5 * width;
                float4 cover = saturate((heads - field) / width + 0.5) * (1.0 - saturate((tails - field) / width + 0.5));

                half4 color = _Backdrop;
                color = lerp(color, Premultiply(_Color0), cover.x);
                color = lerp(color, Premultiply(_Color1), cover.y);
                color = lerp(color, Premultiply(_Color2), cover.z);
                color = lerp(color, fill, cover.w);
                return color;
            }

            #if defined(WIPE_SHAPE)
            // How much a silhouette layer covers a pixel. The silhouette scales about the center, from
            // clearing the rect at a head of 0 down to nothing at 1, and covers what is outside it.
            float ShapeCover(float2 p, float head, float pixel)
            {
                float amount = lerp(1.0 - head, head, _FieldOp.w);
                float scale = max(amount * _FieldA.z, 1e-4);

                float2 d = (p - _FieldA.xy) / scale;
                float2 q = float2(d.x * _FieldB.y - d.y * _FieldB.z, d.x * _FieldB.z + d.y * _FieldB.y);

                // Positive inside the silhouette, in rect heights.
                float dist = tex2Dlod(_ShapeTex, float4(q * 0.5 + 0.5, 0, 0)).r * _FieldB.x;

                // Past the texture a sample repeats its border, which is barely outside where the silhouette
                // reaches the border. Add how far past it is, so the outside keeps getting further away.
                dist = (dist - length(max(abs(q) - 1.0, 0.0))) * scale;
                float inside = saturate(dist / max(pixel, _FieldOp.z) + 0.5);

                return lerp(1.0 - inside, inside, _FieldOp.w);
            }
            #endif

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.local;

            #if defined(WIPE_FILL_TEXTURE)
                half4 fill = tex2D(_Fill, (p * _RectScale.xy + 0.5) * _FillRect.zw + _FillRect.xy);
                fill.rgb *= lerp(fill.a, 1.0, _FillPremultiplied);
                fill *= Premultiply(_ColorFill);
            #else
                half4 fill = Premultiply(_ColorFill);
            #endif

                half4 color;

            #if defined(WIPE_FADE)
                color = lerp(_Backdrop, fill, saturate(_Heads.w * (1.0 - _Tails.w)));
            #elif defined(WIPE_SHAPE)
                float pixel = fwidth(p.y);
                color = _Backdrop;
                color = lerp(color, Premultiply(_Color0), ShapeCover(p, _Heads.x, pixel));
                color = lerp(color, Premultiply(_Color1), ShapeCover(p, _Heads.y, pixel));
                color = lerp(color, Premultiply(_Color2), ShapeCover(p, _Heads.z, pixel));
                color = lerp(color, fill, ShapeCover(p, _Heads.w, pixel));
            #elif defined(WIPE_LANES)
                float2 perpendicular = float2(-_RepeatA.y, _RepeatA.x);
                float across = dot(p, perpendicular) / _RepeatA.z;
                float lane = floor(across);

                // Only the smooth parts set the edge's width. The jump from lane to lane would smear it.
                float smoothPart = _RepeatB.w > 0.5 ? dot(p, _RepeatA.xy) * _RepeatA.w * 0.5 : across;
                float width = fwidth(smoothPart) * (1.0 - _RepeatB.x);

                color = Compose(LaneField(p, lane, across), width, fill);

                // Lanes start at different times, so the line between two lanes is a hard step. Blend
                // with the nearer neighbour over the last pixel to antialias it.
                float inLane = across - lane;
                float neighbour = inLane < 0.5 ? lane - 1.0 : lane + 1.0;
                float edge = (0.5 - abs(inLane - 0.5)) / max(fwidth(across), 1e-5);

                if (edge < 0.5) {
                    half4 other = Compose(LaneField(p, neighbour, across), width, fill);
                    color = lerp(other, color, saturate(edge + 0.5));
                }
            #elif defined(WIPE_GRID)
                // Each cell starts when the wave reaches its center, then its shape grows from the
                // center to the cell's corners.
                float2 g = float2(p.x * _RepeatA.y + p.y * _RepeatA.z, p.y * _RepeatA.y - p.x * _RepeatA.z) / _RepeatA.x;
                float2 cell = floor(g);
                float2 local = frac(g) - 0.5;
                float2 offset = abs(local);

                float2 c = (cell + 0.5) * _RepeatA.x;
                float2 center = float2(c.x * _RepeatA.y - c.y * _RepeatA.z, c.y * _RepeatA.y + c.x * _RepeatA.z);

                float grown = _RepeatB.w < 0.5 ? offset.x + offset.y
                    : (_RepeatB.w < 1.5 ? length(local) * 1.41421356 : max(offset.x, offset.y) * 2.0);

                float field = Delay(center, cell.x + cell.y * 57.0) * _RepeatB.x + grown * (1.0 - _RepeatB.x);
                color = Compose(field, fwidth(grown) * (1.0 - _RepeatB.x), fill);
            #else
                float field = PixelField(p, 0.0);
                float width = fwidth(field);

                #if defined(WIPE_ANGULAR)
                // The field wraps from 1 to 0 at each arm, which is a jump. A copy shifted half an arm
                // jumps somewhere else, so the smaller change is the smooth one.
                width = min(width, fwidth(PixelField(p, 0.5)));
                #endif

                color = Compose(field, width, fill);
            #endif

                return color * IN.alpha;
            }
            ENDCG
        }
    }
}
