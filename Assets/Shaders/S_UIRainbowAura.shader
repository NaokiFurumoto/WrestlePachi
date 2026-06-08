Shader "WrestlePachi/UI/Rainbow Aura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Source Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        // 虹色パラメータ
        _HueSpeed      ("Hue Scroll Speed",   Range(0, 4))    = 0.8
        _Saturation    ("Saturation",         Range(0, 1))    = 1.0
        _Brightness    ("Brightness",         Range(0, 4))    = 2.2
        _HueOffset     ("Hue Offset",         Range(0, 1))    = 0.0

        // グロー
        _GlowSize      ("Glow Size",          Range(0.001, 0.1)) = 0.028
        _GlowPower     ("Glow Power",         Range(0.1, 5))   = 1.3
        _InsideSuppression ("Inside Suppression", Range(0, 1)) = 0.80

        // 光の筋（レイ）
        _RayCount      ("Ray Count",          Range(0, 32))   = 12
        _RaySharpness  ("Ray Sharpness",      Range(0.5, 8))  = 2.5
        _RaySpeed      ("Ray Rotate Speed",   Range(-4, 4))   = 0.6
        _RayAmount     ("Ray Amount",         Range(0, 1))    = 0.55
        _RayOriginY    ("Ray Origin Y",       Range(-0.5, 0.5)) = 0.0

        // スパーク（虹色の粒）
        _SparkDensity  ("Spark Density",      Range(5, 60))   = 28
        _SparkSpeed    ("Spark Speed",        Range(0, 10))   = 3.2

        // パルス
        _PulseSpeed    ("Pulse Speed",        Range(0, 10))   = 3.0
        _PulseMin      ("Pulse Min",          Range(0, 1))    = 0.55

        // マスク
        _SampleScale   ("Source Sample Scale", Range(1, 1.8)) = 1.22
        _AlphaWeight   ("Alpha Mask Weight",  Range(0, 1))    = 1.0
        _LuminanceWeight ("Luminance Mask Weight", Range(0, 1)) = 0.35
        _MaskThreshold ("Mask Threshold",     Range(0, 0.5))  = 0.055

        _EffectAmount  ("Effect Amount",      Range(0, 1))    = 1.0

        [HideInInspector] _StencilComp      ("Stencil Comparison",  Float) = 8
        [HideInInspector] _Stencil          ("Stencil ID",          Float) = 0
        [HideInInspector] _StencilOp        ("Stencil Operation",   Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask",  Float) = 255
        [HideInInspector] _StencilReadMask  ("Stencil Read Mask",   Float) = 255
        [HideInInspector] _ColorMask        ("Color Mask",          Float) = 15
        [HideInInspector] _UseUIAlphaClip   ("Use Alpha Clip",      Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent+10"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    SrcAlpha One   // 加算ブレンドで発光
        ColorMask [_ColorMask]

        Pass
        {
            Name "RainbowAura"

            CGPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            fixed4    _TextureSampleAdd;
            float4    _ClipRect;

            half _HueSpeed;
            half _Saturation;
            half _Brightness;
            half _HueOffset;
            half _GlowSize;
            half _GlowPower;
            half _InsideSuppression;
            half _RayCount;
            half _RaySharpness;
            half _RaySpeed;
            half _RayAmount;
            half _RayOriginY;
            half _SparkDensity;
            half _SparkSpeed;
            half _PulseSpeed;
            half _PulseMin;
            half _SampleScale;
            half _AlphaWeight;
            half _LuminanceWeight;
            half _MaskThreshold;
            half _EffectAmount;

            struct Attributes
            {
                float4 vertex   : POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ──────────────────────────────────────────────
            // ユーティリティ
            // ──────────────────────────────────────────────

            float Hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 74.31);
                return frac(p.x * p.y);
            }

            // HSV → RGB 変換
            half3 HsvToRgb(half h, half s, half v)
            {
                half3 rgb = saturate(abs(frac(h + half3(0.0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0) - 1.0);
                return v * lerp(half3(1,1,1), rgb, s);
            }

            // UV をソーステクスチャ空間に変換（マスク用）
            float2 ToSourceUV(float2 overlayUV)
            {
                return (overlayUV - 0.5) * _SampleScale + 0.5;
            }

            half InUnitSquare(float2 uv)
            {
                half2 inMin = step(0.0, uv);
                half2 inMax = step(uv, 1.0);
                return inMin.x * inMin.y * inMax.x * inMax.y;
            }

            // テクスチャのアルファ・輝度からマスク値を生成
            half MaskAt(float2 sourceUV)
            {
                half inside = InUnitSquare(sourceUV);
                fixed4 s = tex2D(_MainTex, saturate(sourceUV)) + _TextureSampleAdd;
                half lum      = dot(s.rgb, half3(0.299, 0.587, 0.114));
                half alphaMask = smoothstep(0.01, 0.35, s.a) * _AlphaWeight;
                half lumMask   = smoothstep(_MaskThreshold, _MaskThreshold + 0.12, lum) * _LuminanceWeight;
                return saturate(max(alphaMask, lumMask) * inside);
            }

            // 8方向サンプリングで輪郭を膨張
            half NeighbourMask(float2 sourceUV, float radius)
            {
                half m = 0.0;
                m = max(m, MaskAt(sourceUV + float2( radius,  0.0)));
                m = max(m, MaskAt(sourceUV + float2(-radius,  0.0)));
                m = max(m, MaskAt(sourceUV + float2( 0.0,     radius)));
                m = max(m, MaskAt(sourceUV + float2( 0.0,    -radius)));
                m = max(m, MaskAt(sourceUV + float2( radius,  radius) * 0.7071));
                m = max(m, MaskAt(sourceUV + float2(-radius,  radius) * 0.7071));
                m = max(m, MaskAt(sourceUV + float2( radius, -radius) * 0.7071));
                m = max(m, MaskAt(sourceUV + float2(-radius, -radius) * 0.7071));
                return m;
            }

            // ──────────────────────────────────────────────
            // エフェクト
            // ──────────────────────────────────────────────

            // 放射状の光の筋（原点をY方向に移動可能）
            half RayPattern(float2 uv, float time)
            {
                float2 origin = float2(0.5, 0.5 + _RayOriginY);
                float2 centered = uv - origin;
                float angle = atan2(centered.y, centered.x) * (1.0 / (2.0 * 3.14159265));
                float dist  = length(centered);

                float rotAngle = angle + time * _RaySpeed * 0.1;
                // N本の光線：cos を鋭く絞る
                float rays = pow(saturate(cos(rotAngle * _RayCount * 2.0 * 3.14159265)), _RaySharpness);
                // 外縁に向かってフェード
                float fade = exp(-dist * 3.5);
                return rays * fade;
            }

            // 虹色のスパーク粒子
            half SparkPattern(float2 uv, float time)
            {
                float2 g = uv * _SparkDensity;
                float2 cell  = floor(g);
                float2 local = frac(g) - 0.5;

                float rnd  = Hash21(cell);
                float2 jitter = float2(Hash21(cell + 7.3), Hash21(cell + 53.9)) - 0.5;
                float life = frac(rnd + time * _SparkSpeed * (0.15 + rnd * 0.25));
                float2 drift = float2(0.07 * sin(time * 2.1 + rnd * 6.28318), life * 0.45 - 0.22);
                float d = length(local - jitter * 0.38 - drift);

                half shape = 1.0 - smoothstep(0.0, 0.18, d);
                half lifeShape = smoothstep(0.0, 0.15, life) * (1.0 - smoothstep(0.35, 1.0, life));
                half gate = step(0.60, rnd);
                return shape * lifeShape * gate;
            }

            // ──────────────────────────────────────────────
            // 頂点シェーダー
            // ──────────────────────────────────────────────

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = input.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.uv    = TRANSFORM_TEX(input.texcoord, _MainTex);
                o.color = input.color * _Color;
                return o;
            }

            // ──────────────────────────────────────────────
            // フラグメントシェーダー
            // ──────────────────────────────────────────────

            fixed4 Frag(Varyings input) : SV_Target
            {
                float2 sourceUV = ToSourceUV(input.uv);
                float  time     = _Time.y;

                // グロー用マスク（3段階）
                half mask    = MaskAt(sourceUV);
                half near    = NeighbourMask(sourceUV, _GlowSize);
                half mid     = NeighbourMask(sourceUV, _GlowSize * 2.4);
                half far     = NeighbourMask(sourceUV, _GlowSize * 4.6);

                // 輪郭グロー強度
                half outer = saturate(near * 1.1 + mid * 0.65 + far * 0.25 - mask * _InsideSuppression);
                outer = pow(saturate(outer), _GlowPower);

                // パルス
                half pulse = lerp(_PulseMin, 1.0, (sin(time * _PulseSpeed) + 1.0) * 0.5);

                // 光の筋
                half rays  = RayPattern(input.uv, time) * _RayAmount * saturate(near * 0.8 + far * 0.4);

                // スパーク粒子
                half sparks = SparkPattern(input.uv, time) * saturate(mid + far - mask * 0.65);

                // ──────────────────────────────────────────
                // 虹色を位置 + 時間でスクロール
                // ──────────────────────────────────────────

                // グロー輪郭の虹色（上→下に色が流れる）
                float glowHue  = input.uv.y + time * _HueSpeed * 0.1 + _HueOffset;
                half3 glowRgb  = HsvToRgb(frac(glowHue), _Saturation, _Brightness);

                // 光の筋の虹色（レイ原点からの角度方向に色が分かれる）
                float2 c2 = input.uv - float2(0.5, 0.5 + _RayOriginY);
                float rayHue = atan2(c2.y, c2.x) * (1.0 / (2.0 * 3.14159265))
                             + time * _HueSpeed * 0.08 + _HueOffset;
                half3 rayRgb  = HsvToRgb(frac(rayHue), _Saturation, _Brightness);

                // スパークの虹色（各粒子セルごとに色が変わる）
                float sparkHue = Hash21(floor(input.uv * _SparkDensity))
                               + time * _HueSpeed * 0.12 + _HueOffset;
                half3 sparkRgb = HsvToRgb(frac(sparkHue), _Saturation, _Brightness);

                // ──────────────────────────────────────────
                // 合成
                // ──────────────────────────────────────────

                half effect = saturate(_EffectAmount);

                half glowAlpha  = outer * 0.55 * pulse;
                half rayAlpha   = rays  * 1.0;
                half sparkAlpha = sparks * 0.85;
                half alpha = saturate((glowAlpha + rayAlpha + sparkAlpha) * effect * input.color.a);

                half3 color = (glowRgb  * glowAlpha
                             + rayRgb   * rayAlpha
                             + sparkRgb * sparkAlpha)
                             * effect * input.color.rgb;

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}
