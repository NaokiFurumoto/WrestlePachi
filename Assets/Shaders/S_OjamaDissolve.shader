Shader "Custom/S_OjamaDissolve"
{
    Properties
    {
        _MainTex   ("Sprite",      2D)           = "white" {}
        _Cutoff    ("Cutoff",      Range(0,1))   = 0
        _EdgeColor ("Edge Color",  Color)        = (1, 0.5, 0, 1)
        _EdgeWidth ("Edge Width",  Range(0,0.3)) = 0.08
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalRenderPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float  _Cutoff;
            float4 _EdgeColor;
            float  _EdgeWidth;
            float4 _Color;
            CBUFFER_END

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

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv          = TRANSFORM_TEX(v.uv, _MainTex);
                o.color       = v.color * _Color;
                return o;
            }

            // テクスチャ不要のハッシュノイズ
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 uv)
            {
                float2 ip = floor(uv * 8.0);
                float2 fp = frac(uv * 8.0);
                fp = fp * fp * (3.0 - 2.0 * fp);

                float a = hash(ip);
                float b = hash(ip + float2(1, 0));
                float c = hash(ip + float2(0, 1));
                float d = hash(ip + float2(1, 1));

                return lerp(lerp(a, b, fp.x), lerp(c, d, fp.x), fp.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;

                // スプライトの alpha が 0 のピクセルは最初からカット
                clip(col.a - 0.01);

                float n = noise(i.uv);
                clip(n - _Cutoff);

                // Cutoff エッジに発光色を乗せる
                float edge = saturate((n - _Cutoff) / max(_EdgeWidth, 0.001));
                col.rgb = lerp(_EdgeColor.rgb, col.rgb, edge);

                return col;
            }
            ENDHLSL
        }
    }
}
