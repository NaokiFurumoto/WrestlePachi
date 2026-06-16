Shader "App/ClearSparkle"
{
    Properties
    {
        _MainTex           ("Sprite Texture",    2D)           = "white" {}
        [HDR] _ShimmerColor("Shimmer Color",     Color)        = (1, 0.95, 0.7, 1)
        _ShimmerSpeed      ("Shimmer Speed",     Float)        = 1.2
        _ShimmerWidth      ("Shimmer Width",     Range(0,0.5)) = 0.08
        _ShimmerSoftness   ("Shimmer Softness",  Range(0,0.3)) = 0.04
        _SparkleGrid       ("Sparkle Grid",      Float)        = 18
        _SparkleSpeed      ("Sparkle Speed",     Float)        = 2.5
        _SparkleIntensity  ("Sparkle Intensity", Float)        = 1.8
        _PulseSpeed        ("Pulse Speed",       Float)        = 1.4
        _PulseAmplitude    ("Pulse Amplitude",   Range(0,0.4)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ShimmerColor;
                float  _ShimmerSpeed;
                float  _ShimmerWidth;
                float  _ShimmerSoftness;
                float  _SparkleGrid;
                float  _SparkleSpeed;
                float  _SparkleIntensity;
                float  _PulseSpeed;
                float  _PulseAmplitude;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                return OUT;
            }

            // セルごとに異なるランダム値を返す（0〜1）
            float Hash(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // テクスチャの alpha が 0 のピクセルはそのまま透明
                clip(tex.a - 0.01);

                float t = _Time.y;

                // ── 1. パルス ───────────────────────────────────────
                // 全体輝度を sin 波で上下させる
                float pulse = 1.0 + _PulseAmplitude * sin(t * _PulseSpeed * 6.2832);

                // ── 2. シマー（光の帯） ──────────────────────────────
                // 斜め45°方向にスクロールする帯（UV.x + UV.y を軸に使う）
                float shimmerAxis   = IN.uv.x - IN.uv.y * 0.4;
                float shimmerPhase  = frac(shimmerAxis - t * _ShimmerSpeed * 0.25);
                // 帯の中心 0.5 にスムーズステップで絞る
                float halfW  = _ShimmerWidth * 0.5;
                float halfS  = _ShimmerSoftness;
                float shimmer = smoothstep(0.5 - halfW - halfS, 0.5 - halfW, shimmerPhase)
                              - smoothstep(0.5 + halfW,         0.5 + halfW + halfS, shimmerPhase);
                // テクスチャの明るい部分にだけ乗せる
                half lum = dot(tex.rgb, half3(0.299, 0.587, 0.114));
                shimmer *= lum * lum; // 暗い部分（影・縁）には乗らない

                // ── 3. スパークル ────────────────────────────────────
                // UV をグリッドに分割し、セルごとにランダムな位相でキラキラ点滅
                float2 sparkleCell  = floor(IN.uv * _SparkleGrid);
                float  sparklePhase = Hash(sparkleCell);                      // 0〜1 のランダム位相
                float  sparkleSin   = sin(t * _SparkleSpeed * 6.2832 + sparklePhase * 6.2832);
                float  sparkle      = pow(max(sparkleSin, 0.0), 8.0);        // 鋭い点滅にする

                // セル内での中心距離でやわらかくする
                float2 cellUV  = frac(IN.uv * _SparkleGrid) - 0.5;
                float  cellDot = dot(cellUV, cellUV);
                sparkle *= saturate(1.0 - cellDot * 8.0);                    // セル中心のみ光る

                sparkle *= lum;                                               // テクスチャの明部のみ

                // ── 合成 ─────────────────────────────────────────────
                half3 shimmerRgb  = _ShimmerColor.rgb * shimmer  * _ShimmerColor.a;
                half3 sparkleRgb  = _ShimmerColor.rgb * sparkle  * _SparkleIntensity;

                half3 rgb = tex.rgb * IN.color.rgb * pulse + shimmerRgb + sparkleRgb;

                return half4(rgb, tex.a * IN.color.a);
            }
            ENDHLSL
        }
    }
}
