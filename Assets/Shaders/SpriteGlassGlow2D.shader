Shader "WrestlePachi/2D/Sprite Glass Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Renderer Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)

        [Enum(Custom,0,Red,1,Blue,2,Yellow,3,Green,4,Rainbow,5)] _ColorMode ("Color Mode", Float) = 5
        [HDR] _TintColor ("Custom Color", Color) = (0.25, 0.75, 1, 1)
        [HDR] _SecondaryColor ("Secondary Color", Color) = (1, 1, 1, 1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0.82
        _EffectAmount ("Effect Amount", Range(0, 1)) = 0
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.6
        _GlassStrength ("Glass Strength", Range(0, 1)) = 0.75
        _HighlightStrength ("Highlight Strength", Range(0, 2)) = 0.9
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.8

        _WaveStrength ("Wave Strength", Range(0, 0.08)) = 0.012
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1.2
        _WaveFrequency ("Wave Frequency", Range(1, 60)) = 18
        _SweepSpeed ("Sweep Speed", Range(0, 30)) = 7.5
        _SweepWidth ("Sweep Width", Range(0.005, 0.4)) = 0.08
        _PulseSpeed ("Pulse Speed", Range(0, 8)) = 2.3
        _RainbowSpeed ("Rainbow Speed", Range(0, 2)) = 0.16
        _RainbowScale ("Rainbow Scale", Range(0.1, 6)) = 1.2
        _Alpha ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            half4 _TintColor;
            half4 _SecondaryColor;
            half _ColorMode;
            half _TintStrength;
            half _EffectAmount;
            half _GlowIntensity;
            half _GlassStrength;
            half _HighlightStrength;
            half _RimStrength;
            half _WaveStrength;
            half _WaveSpeed;
            half _WaveFrequency;
            half _SweepSpeed;
            half _SweepWidth;
            half _PulseSpeed;
            half _RainbowSpeed;
            half _RainbowScale;
            half _Alpha;
        CBUFFER_END

        half4 _RendererColor;
        float4 _Flip;

        struct Attributes
        {
            float3 positionOS : POSITION;
            half4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half4 color : COLOR;
            float2 uv : TEXCOORD0;
            float2 spriteUV : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        half3 HsvToRgb(half3 c)
        {
            half4 k = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            half3 p = abs(frac(c.xxx + k.xyz) * 6.0 - k.www);
            return c.z * lerp(k.xxx, saturate(p - k.xxx), c.y);
        }

        half3 PresetColor(half mode, float2 uv, float time, half wave)
        {
            if (mode > 4.5)
            {
                half hue = frac(uv.x * _RainbowScale + uv.y * 0.35 + time * _RainbowSpeed + wave * 0.035);
                return HsvToRgb(half3(hue, 0.86, 1.0));
            }

            if (mode > 3.5) return half3(0.12, 1.0, 0.32);
            if (mode > 2.5) return half3(1.0, 0.86, 0.12);
            if (mode > 1.5) return half3(0.10, 0.48, 1.0);
            if (mode > 0.5) return half3(1.0, 0.08, 0.06);

            return _TintColor.rgb;
        }

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            float3 positionOS = input.positionOS;
            positionOS.xy *= _Flip.xy;

            output.positionCS = TransformObjectToHClip(positionOS);
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);
            output.spriteUV = input.uv;
            output.color = input.color * _Color * _RendererColor;
            return output;
        }

        half4 Frag(Varyings input) : SV_Target
        {
            float2 uv = input.spriteUV;
            float time = _Time.y;
            half effect = saturate(_EffectAmount);

            half waveA = sin((uv.y * _WaveFrequency + time * _WaveSpeed) + sin(uv.x * 6.28318 + time * 0.6));
            half waveB = cos((uv.x * (_WaveFrequency * 0.72) - time * _WaveSpeed * 1.12) + uv.y * 4.0);
            float2 waveOffset = float2(waveA, waveB) * _WaveStrength;

            half4 original = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
            half4 warped = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, lerp(input.uv, saturate(input.uv + waveOffset), effect)) * input.color;
            half alpha = original.a * lerp(1.0, _Alpha, effect);

            half luminance = dot(warped.rgb, half3(0.299, 0.587, 0.114));
            half3 modeColor = PresetColor(_ColorMode, uv, time, waveA);

            half secondaryMix = saturate((sin((uv.x * 0.8 + uv.y * 1.25) * 6.28318 + time * _WaveSpeed) * 0.5 + 0.5) * _GlassStrength);
            half3 glassColor = lerp(modeColor, _SecondaryColor.rgb, secondaryMix * 0.28);

            half3 tinted = glassColor * (0.28 + luminance * 1.35);
            half3 color = lerp(warped.rgb, tinted, _TintStrength);

            half sweepCenter = 1.0 - frac(time * _SweepSpeed);
            half sweepDist = abs((uv.y + waveA * 0.018) - sweepCenter);
            half sweepShape = saturate(1.0 - sweepDist / max(_SweepWidth, 0.001));
            half sweep = pow(sweepShape, 2.5) * _HighlightStrength;
            half streakShape = sin((uv.x + waveA * 0.025) * 76.0 + time * _WaveSpeed * 1.8) * 0.5 + 0.5;
            half streak = pow(saturate(streakShape), 14.0) * _HighlightStrength * (0.22 + luminance * 0.55);

            float2 pixelStep = max(fwidth(input.uv) * 2.0, 0.001);
            half edgeRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(pixelStep.x, 0)).a;
            half edgeLeft = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(pixelStep.x, 0)).a;
            half edgeUp = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, pixelStep.y)).a;
            half edgeDown = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(0, pixelStep.y)).a;
            half edge = saturate((alpha - min(min(edgeRight, edgeLeft), min(edgeUp, edgeDown))) * 5.0);

            half pulse = 0.72 + 0.28 * sin(time * _PulseSpeed + uv.y * 6.28318);
            half glowMask = saturate(luminance * 0.68 + sweep * 0.55 + streak * 0.35 + edge * _RimStrength);
            half3 emission = glassColor * glowMask * _GlowIntensity * pulse;

            half3 highlight = (sweep + streak) * (0.75 + _GlassStrength) + edge * glassColor * _RimStrength;
            color = color + highlight + emission;
            color = lerp(original.rgb, color, effect);

            return half4(color, alpha);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
