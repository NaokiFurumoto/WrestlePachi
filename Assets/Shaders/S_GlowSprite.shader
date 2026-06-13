Shader "App/GlowSprite"
{
    Properties
    {
        _MainTex          ("Sprite Texture", 2D)          = "white" {}
        [HDR] _GlowColor  ("Glow Color",     Color)       = (1, 0.3, 0, 1)
        _GlowIntensity    ("Glow Intensity",  Float)       = 1.5
        _LumCutoff        ("Luminance Cutoff", Range(0,1)) = 0.15
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

        // 加算合成：背景に輝度を加えて発光に見せる
        Blend One One
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
                float4 _GlowColor;
                float  _GlowIntensity;
                float  _LumCutoff;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // 輝度が _LumCutoff 未満のピクセルを切り捨てて「衣」を除去
                half lum = dot(tex.rgb, half3(0.299, 0.587, 0.114));
                clip(lum - _LumCutoff);

                // vertex color の alpha（SpriteRenderer.color.a）を輝度に掛けてフェードアウトを制御
                half3 rgb = tex.rgb * IN.color.rgb
                          * _GlowColor.rgb * _GlowIntensity
                          * IN.color.a;

                // Additive なので alpha は 1 固定（ブレンド式：src.rgb * 1 + dst.rgb * 1）
                return half4(rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
