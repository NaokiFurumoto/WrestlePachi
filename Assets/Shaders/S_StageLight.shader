Shader "App/StageLight"
{
    Properties
    {
        _MainTex       ("Sprite Texture",   2D)           = "white" {}
        [HDR] _Color   ("Light Color",      Color)        = (1, 0.85, 0.3, 1)
        _ConeSharpness ("Cone Sharpness",   Range(1, 8))  = 3.0
        _NoiseScale    ("Noise Scale",      Float)        = 6.0
        _NoiseSpeed    ("Noise Speed",      Float)        = 0.25
        _NoiseStrength ("Noise Strength",   Range(0, 0.5))= 0.22
        _AlphaTop      ("Alpha at Top",     Range(0, 1))  = 0.85
        _AlphaBottom   ("Alpha at Bottom",  Range(0, 1))  = 0.0
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
                float4 _Color;
                float  _ConeSharpness;
                float  _NoiseScale;
                float  _NoiseSpeed;
                float  _NoiseStrength;
                float  _AlphaTop;
                float  _AlphaBottom;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                return OUT;
            }

            // シンプルな値ノイズ（縦方向の光の筋用）
            float Hash(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep
                return lerp(
                    lerp(Hash(i),                Hash(i + float2(1, 0)), f.x),
                    lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x),
                    f.y
                );
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // ── コーン形状 ────────────────────────────────────
                // UV.x=0.5 が中心。端に向かって絞る。
                // UV.y=1 が上端（光源）、UV.y=0 が下端（床）。
                // 下（UV.y=0）に向かってコーンが広がる。
                float halfWidth = lerp(0.02, 0.5, 1.0 - uv.y); // 上端で細く、下端で全幅
                float dist      = abs(uv.x - 0.5);
                float cone      = 1.0 - smoothstep(halfWidth * 0.6, halfWidth, dist);
                cone            = pow(cone, _ConeSharpness);

                // ── 縦方向ノイズで光の筋 ─────────────────────────
                float t         = _Time.y * _NoiseSpeed;
                float noiseUV_x = floor(uv.x * _NoiseScale) / _NoiseScale; // 縦の帯に分割
                float noise     = ValueNoise(float2(noiseUV_x * _NoiseScale, uv.y * 2.0 - t));
                noise           = lerp(1.0, noise, _NoiseStrength);

                // ── 縦グラデーション ─────────────────────────────
                // UV.y=1（上）が明るく、UV.y=0（下）が暗い
                float gradient  = lerp(_AlphaBottom, _AlphaTop, uv.y);

                // ── 合成 ─────────────────────────────────────────
                float alpha     = cone * gradient * noise * IN.color.a;
                half3 rgb       = _Color.rgb * IN.color.rgb;

                return half4(rgb, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
