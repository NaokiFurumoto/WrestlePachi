Shader "WrestlePachi/UI/Glass Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Source Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        // カラーモード
        [Enum(Custom,0,Red,1,Blue,2,Yellow,3,Green,4,Rainbow,5)]
        _ColorMode         ("Color Mode",         Float)          = 5
        [HDR] _TintColor   ("Custom Color",       Color)          = (0.25, 0.75, 1, 1)
        [HDR] _SecondaryColor ("Secondary Color", Color)          = (1, 1, 1, 1)
        _TintStrength      ("Tint Strength",       Range(0, 1))   = 0.82
        _EffectAmount      ("Effect Amount",       Range(0, 1))   = 1.0

        // ガラス光沢
        _GlowIntensity     ("Glow Intensity",      Range(0, 5))   = 1.6
        _GlassStrength     ("Glass Strength",      Range(0, 1))   = 0.75
        _HighlightStrength ("Highlight Strength",  Range(0, 2))   = 0.9
        _RimStrength       ("Rim Strength",        Range(0, 2))   = 0.8

        // 波ゆらぎ
        _WaveStrength      ("Wave Strength",       Range(0, 0.08)) = 0.012
        _WaveSpeed         ("Wave Speed",          Range(0, 5))   = 1.2
        _WaveFrequency     ("Wave Frequency",      Range(1, 60))  = 18

        // スイープハイライト
        _SweepSpeed        ("Sweep Speed",         Range(0, 30))  = 7.5
        _SweepWidth        ("Sweep Width",         Range(0.005, 0.4)) = 0.08

        // パルス
        _PulseSpeed        ("Pulse Speed",         Range(0, 8))   = 2.3

        // レインボーモード用
        _RainbowSpeed      ("Rainbow Speed",       Range(0, 2))   = 0.16
        _RainbowScale      ("Rainbow Scale",       Range(0.1, 6)) = 1.2

        _Alpha             ("Alpha",               Range(0, 1))   = 1.0

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
            "Queue"             = "Transparent"
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
        Blend    SrcAlpha OneMinusSrcAlpha  // 通常合成（ガラス透過）
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIGlassGlow"

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

            half  _ColorMode;
            fixed4 _TintColor;
            fixed4 _SecondaryColor;
            half  _TintStrength;
            half  _EffectAmount;
            half  _GlowIntensity;
            half  _GlassStrength;
            half  _HighlightStrength;
            half  _RimStrength;
            half  _WaveStrength;
            half  _WaveSpeed;
            half  _WaveFrequency;
            half  _SweepSpeed;
            half  _SweepWidth;
            half  _PulseSpeed;
            half  _RainbowSpeed;
            half  _RainbowScale;
            half  _Alpha;

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

            // HSV → RGB
            half3 HsvToRgb(half h, half s, half v)
            {
                half3 rgb = saturate(abs(frac(h + half3(0.0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0) - 1.0);
                return v * lerp(half3(1,1,1), rgb, s);
            }

            // カラーモードに応じた基本色を返す
            half3 ModeColor(half mode, float2 uv, float time, half wave)
            {
                if (mode > 4.5)
                {
                    // レインボー：上→下に色が流れる
                    half hue = frac(uv.y * _RainbowScale + time * _RainbowSpeed + wave * 0.035);
                    return HsvToRgb(hue, 0.86, 1.0);
                }
                if (mode > 3.5) return half3(0.12, 1.0, 0.32);  // Green
                if (mode > 2.5) return half3(1.0,  0.86, 0.12); // Yellow
                if (mode > 1.5) return half3(0.10, 0.48, 1.0);  // Blue
                if (mode > 0.5) return half3(1.0,  0.08, 0.06); // Red
                return _TintColor.rgb;                            // Custom
            }

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

            fixed4 Frag(Varyings input) : SV_Target
            {
                float2 uv   = input.uv;
                float  time = _Time.y;
                half   effect = saturate(_EffectAmount);

                // ── 波ゆらぎ ──────────────────────────────────
                half waveA = sin((uv.y * _WaveFrequency + time * _WaveSpeed)
                               + sin(uv.x * 6.28318 + time * 0.6));
                half waveB = cos((uv.x * (_WaveFrequency * 0.72) - time * _WaveSpeed * 1.12)
                               + uv.y * 4.0);
                float2 waveOffset = float2(waveA, waveB) * _WaveStrength;

                fixed4 original = tex2D(_MainTex, uv)              * input.color + _TextureSampleAdd;
                fixed4 warped   = tex2D(_MainTex, saturate(uv + waveOffset * effect)) * input.color + _TextureSampleAdd;

                half alpha = original.a * lerp(1.0, _Alpha, effect);

                // ── ガラス着色 ────────────────────────────────
                half  luminance   = dot(warped.rgb, half3(0.299, 0.587, 0.114));
                half3 modeColor   = ModeColor(_ColorMode, uv, time, waveA);

                half secondaryMix = saturate((sin((uv.x * 0.8 + uv.y * 1.25) * 6.28318
                                               + time * _WaveSpeed) * 0.5 + 0.5) * _GlassStrength);
                half3 glassColor  = lerp(modeColor, _SecondaryColor.rgb, secondaryMix * 0.28);

                half3 tinted = glassColor * (0.28 + luminance * 1.35);
                half3 color  = lerp(warped.rgb, tinted, _TintStrength * effect);

                // ── スイープハイライト（横線が流れ落ちる）────
                half sweepCenter = 1.0 - frac(time * _SweepSpeed);
                half sweepDist   = abs((uv.y + waveA * 0.018) - sweepCenter);
                half sweepShape  = saturate(1.0 - sweepDist / max(_SweepWidth, 0.001));
                half sweep       = pow(sweepShape, 2.5) * _HighlightStrength;

                // ── ストリーク（縦スジ光沢）──────────────────
                half streakShape = sin((uv.x + waveA * 0.025) * 76.0 + time * _WaveSpeed * 1.8) * 0.5 + 0.5;
                half streak      = pow(saturate(streakShape), 14.0) * _HighlightStrength * (0.22 + luminance * 0.55);

                // ── リムエッジ（輪郭ライン）──────────────────
                float2 px = max(abs(ddx(uv)), abs(ddy(uv))) * 2.0;
                px = max(px, 0.001);
                half eR = tex2D(_MainTex, uv + float2( px.x,  0.0)).a;
                half eL = tex2D(_MainTex, uv + float2(-px.x,  0.0)).a;
                half eU = tex2D(_MainTex, uv + float2( 0.0,   px.y)).a;
                half eD = tex2D(_MainTex, uv + float2( 0.0,  -px.y)).a;
                half edge = saturate((original.a - min(min(eR, eL), min(eU, eD))) * 5.0);

                // ── パルス発光 ────────────────────────────────
                half pulse    = 0.72 + 0.28 * sin(time * _PulseSpeed + uv.y * 6.28318);
                half glowMask = saturate(luminance * 0.68 + sweep * 0.55 + streak * 0.35 + edge * _RimStrength);
                half3 emission = glassColor * glowMask * _GlowIntensity * pulse * effect;

                // ── 合成 ─────────────────────────────────────
                half3 highlight = (sweep + streak) * (0.75 + _GlassStrength) + edge * glassColor * _RimStrength;
                color = color + (highlight + emission) * effect;
                color = lerp(original.rgb, color, effect);

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
