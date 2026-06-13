Shader "App/PuyoDissolve"
{
    Properties
    {
        _MainTex    ("Sprite Texture", 2D)          = "white" {}
        _Dissolve   ("Dissolve Amount", Range(0, 1)) = 0
        _EdgeColor  ("Edge Color",      Color)       = (1, 0.5, 0, 1)
        _EdgeWidth  ("Edge Width",      Range(0, 0.2)) = 0.05
        _NoiseScale ("Noise Scale",     Float)       = 30
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
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
                // _MainTex_ST は 2D SRP Batcher 非対応のため除外
                float  _Dissolve;
                float4 _EdgeColor;
                float  _EdgeWidth;
                float  _NoiseScale;
            CBUFFER_END

            // ── Value Noise ─────────────────────────────────────────────
            float _Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float _SmoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = _Hash(i);
                float b = _Hash(i + float2(1, 0));
                float c = _Hash(i + float2(0, 1));
                float d = _Hash(i + float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // ── Vertex ──────────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv; // スプライトのUVはアトラス空間なのでそのまま使う
                OUT.color       = IN.color;
                return OUT;
            }

            // ── Fragment ────────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                // ノイズ値（0〜1）
                float n = _SmoothNoise(IN.uv * _NoiseScale);

                // _Dissolve 以下のピクセルを切り捨て
                clip(n - _Dissolve);

                // 境界線（_Dissolve 〜 _Dissolve + _EdgeWidth）に発光色を加算
                float edgeFactor = smoothstep(_Dissolve, _Dissolve + _EdgeWidth, n);
                texColor.rgb    += _EdgeColor.rgb * _EdgeColor.a * (1.0 - edgeFactor);

                return texColor;
            }
            ENDHLSL
        }
    }
}
