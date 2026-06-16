// 画面全体にラジアルブラー（中心から放射状）をかけるフルスクリーンシェーダー。
// URP 2D Renderer の ScriptableRendererFeature（RadialBlurFeature）から使用する。
Shader "Hidden/WrestlePachi/RadialBlur"
{
    Properties
    {
        _Intensity   ("Intensity",    Range(0, 0.5)) = 0
        _SampleCount ("Sample Count", Range(2, 16))  = 8
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "RadialBlur"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;
            int   _SampleCount;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                if (_Intensity < 0.001)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 dir = uv - float2(0.5, 0.5);
                int    n   = max(_SampleCount, 2);
                half4  col = 0;

                UNITY_LOOP
                for (int i = 0; i < n; i++)
                {
                    float  t        = (float)i / (float)(n - 1);
                    float2 sampleUV = uv - dir * _Intensity * t;
                    col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUV);
                }

                return col / (half)n;
            }
            ENDHLSL
        }
    }
}
