// 炎の揺らめきシェーダー（敵スキル「炎」の炎マス用）
// ・根元は固定したまま、上に行くほど左右に大きく波打つ
// ・上へ流れるノイズで輪郭がちらちら燃えるように動く
// ・明るさが小刻みに明滅する
// ・炎ごとに揺れのタイミングがずれるよう、ワールド座標から位相を作る
Shader "App/FireFlicker"
{
    Properties
    {
        _MainTex       ("Sprite Texture", 2D)            = "white" {}
        [Header(Wave)]
        _WaveStrength  ("Wave Strength (UV)", Range(0, 0.15)) = 0.045
        _WaveFrequency ("Wave Frequency", Float)          = 7
        _WaveSpeed     ("Wave Speed", Float)              = 6
        [Header(Noise)]
        _NoiseStrength ("Noise Strength (UV)", Range(0, 0.1)) = 0.03
        _NoiseScale    ("Noise Scale", Float)             = 6
        _NoiseSpeed    ("Noise Rise Speed", Float)        = 2.5
        [Header(Flicker)]
        _FlickerAmount ("Brightness Flicker", Range(0, 0.6)) = 0.18
        _FlickerSpeed  ("Flicker Speed", Float)           = 11
        [Header(Shape)]
        _RootHeight    ("Fixed Root Height (UV)", Range(0, 0.6)) = 0.15
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
                float  phase       : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _WaveStrength;
                float _WaveFrequency;
                float _WaveSpeed;
                float _NoiseStrength;
                float _NoiseScale;
                float _NoiseSpeed;
                float _FlickerAmount;
                float _FlickerSpeed;
                float _RootHeight;
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
                float2 u = f * f * (3.0 - 2.0 * f);

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
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                // 炎ごとに揺れをずらすための位相（オブジェクトのワールド位置から作る）
                float3 origin = TransformObjectToWorld(float3(0, 0, 0));
                OUT.phase     = _Hash(floor(origin.xy * 10.0)) * 6.2831;
                return OUT;
            }

            // ── Fragment ────────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y;

                // 根元（_RootHeight より下）は動かさず、上に行くほど大きく揺らす
                float h      = saturate((IN.uv.y - _RootHeight) / max(1.0 - _RootHeight, 0.001));
                float weight = h * h;

                // 左右の波打ち（下から上へ波が登っていく）
                float wave = sin(IN.uv.y * _WaveFrequency - t * _WaveSpeed + IN.phase) * _WaveStrength;

                // 上へ流れるノイズで輪郭をちらつかせる
                float2 noiseUV = IN.uv * _NoiseScale + float2(IN.phase, -t * _NoiseSpeed);
                float  nx      = _SmoothNoise(noiseUV) - 0.5;
                float  ny      = _SmoothNoise(noiseUV + 17.3) - 0.5;

                float2 offset = float2(wave + nx * 2.0 * _NoiseStrength, ny * _NoiseStrength) * weight;
                half4  col    = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + offset) * IN.color;

                // 明るさの明滅（ゆっくりした波＋細かいゆらぎ）
                float flicker = 1.0
                    + sin(t * _FlickerSpeed + IN.phase) * _FlickerAmount * 0.5
                    + (_SmoothNoise(float2(t * _FlickerSpeed * 0.35, IN.phase)) - 0.5) * _FlickerAmount;
                col.rgb *= flicker;

                return col;
            }
            ENDHLSL
        }
    }
}
