Shader "WrestlePachi/UI/EnemyHpBar"
{
    Properties
    {
        [PerRendererData] _MainTex ("Source Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        // HP割合（スクリプトから更新）
        _FillAmount ("Fill Amount", Range(0, 1)) = 1.0

        // HP段階別カラー（HDR で発光感）
        [HDR] _HighColor ("High HP Color", Color) = (0.12, 1.0, 0.32, 1)
        [HDR] _MidColor  ("Mid HP Color",  Color) = (1.0, 0.86, 0.12, 1)
        [HDR] _LowColor  ("Low HP Color",  Color) = (1.0, 0.15, 0.08, 1)

        // 閾値（0〜1）
        _LowThreshold  ("Low Threshold",  Range(0, 1)) = 0.3
        _HighThreshold ("High Threshold", Range(0, 1)) = 0.6

        // 空白部分の色
        _EmptyColor ("Empty Color", Color) = (0.08, 0.08, 0.10, 1)

        // エフェクト調整
        _GlowIntensity ("Glow Intensity", Range(0.5, 3.0)) = 1.4
        _GlossStrength ("Gloss Strength", Range(0.0, 1.0)) = 0.65
        _PulseSpeed    ("Pulse Speed",    Range(0.0, 5.0)) = 1.8

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
        Blend    SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "EnemyHpBar"

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

            half   _FillAmount;
            fixed4 _HighColor;
            fixed4 _MidColor;
            fixed4 _LowColor;
            half   _LowThreshold;
            half   _HighThreshold;
            fixed4 _EmptyColor;
            half   _GlowIntensity;
            half   _GlossStrength;
            half   _PulseSpeed;

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

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = input.vertex;
                o.vertex        = UnityObjectToClipPos(o.worldPosition);
                o.uv            = TRANSFORM_TEX(input.texcoord, _MainTex);
                o.color         = input.color * _Color;
                return o;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                float2 uv  = input.uv;
                float  time = _Time.y;

                fixed4 tex = tex2D(_MainTex, uv) * input.color + _TextureSampleAdd;

                // ── HP割合に基づく色グラデーション ───────────────────
                half t        = _FillAmount;
                half lowT     = saturate(t / max(_LowThreshold, 0.001));
                half midHighT = saturate((t - _LowThreshold)
                              / max(_HighThreshold - _LowThreshold, 0.001));
                half3 lowToMid  = lerp(_LowColor.rgb, _MidColor.rgb,  lowT);
                half3 midToHigh = lerp(_MidColor.rgb, _HighColor.rgb, midHighT);
                half3 baseColor = lerp(lowToMid, midToHigh, step(_LowThreshold, t));

                // ── スキャンライン（SF感の横縞）──────────────────────
                float scanline = 0.78 + 0.22 * saturate(sin(uv.y * 55.0 * 3.14159));

                // ── 流れる光のストリーク（3本・速さ違い）────────────
                // UV.x を時間でオフセットして左→右へ流れる光の筋を作る
                float f1 = frac(uv.x - time * 0.85);
                float s1 = pow(max(0.0, 1.0 - abs(f1 - 0.5) * 4.8), 3.5) * 1.4;

                float f2 = frac(uv.x - time * 0.52 + 0.38);
                float s2 = pow(max(0.0, 1.0 - abs(f2 - 0.5) * 6.5), 3.0) * 0.9;

                float f3 = frac(uv.x * 1.8 - time * 1.3 + 0.7);
                float s3 = pow(max(0.0, 1.0 - abs(f3 - 0.5) * 9.0), 4.5) * 0.6;

                float streaks = min(s1 + s2 + s3, 2.2);

                // ── 上下エッジの発光ライン ────────────────────────────
                // バーの上端・下端が明るく輝くラインになる
                float edgeY   = pow(saturate(abs(uv.y - 0.5) * 2.4 - 0.7), 1.5);
                half3 edgeCol = baseColor * edgeY * 3.0 * _GlossStrength;

                // ── 内部グロー（中央が微かに明るい）─────────────────
                float innerY  = pow(saturate(1.0 - abs(uv.y - 0.5) * 2.8), 0.6) * 0.5;

                // ── パルス（低HPほど速く点滅）────────────────────────
                half pulseSpeed = _PulseSpeed * (1.0 + (1.0 - t) * 2.0);
                float pulse     = 0.82 + 0.18 * sin(time * pulseSpeed);

                // ── 合成（ベース暗め + ストリーク + エッジ + 内部グロー）
                half3 barColor  = baseColor * 0.45 * scanline;       // 暗いベース
                barColor       += baseColor * streaks * pulse;        // 流れる光
                barColor       += edgeCol;                            // 上下エッジライン
                barColor       += baseColor * innerY;                 // 内部グロー
                barColor       *= _GlowIntensity;

                // ── 塗り判定 ─────────────────────────────────────────
                float filled = step(uv.x, _FillAmount);

                // ── 塗り端の垂直発光ライン ────────────────────────────
                float edgeDist = (_FillAmount - uv.x) / 0.012;
                float edgeGlow = saturate(1.0 - edgeDist) * filled;
                barColor += baseColor * edgeGlow * 2.5;

                // ── 空白部分と合成 ────────────────────────────────────
                half3 finalColor = lerp(_EmptyColor.rgb, barColor, filled);

                float alpha = tex.a;

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
}
