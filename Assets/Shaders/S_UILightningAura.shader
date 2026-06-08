Shader "WrestlePachi/UI/Lightning Aura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Source Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [HDR] _AuraColor ("Aura Color", Color) = (1.0, 0.45, 0.02, 1)
        [HDR] _LightningColor ("Lightning Color", Color) = (1.0, 0.95, 0.45, 1)
        [HDR] _CoreColor ("Core Color", Color) = (0.18, 0.75, 1.0, 1)

        _EffectAmount ("Effect Amount", Range(0, 1)) = 1
        _Intensity ("Intensity", Range(0, 10)) = 3.2
        _SampleScale ("Source Sample Scale", Range(1, 1.8)) = 1.22

        _AlphaWeight ("Alpha Mask Weight", Range(0, 1)) = 1
        _LuminanceWeight ("Luminance Mask Weight", Range(0, 1)) = 0.35
        _MaskThreshold ("Mask Threshold", Range(0, 0.5)) = 0.055

        _GlowSize ("Glow Size", Range(0.001, 0.08)) = 0.024
        _GlowPower ("Glow Power", Range(0.1, 6)) = 1.45
        _InsideSuppression ("Inside Suppression", Range(0, 1)) = 0.86

        _LightningDensity ("Lightning Density", Range(2, 28)) = 11
        _LightningWidth ("Lightning Width", Range(0.001, 0.12)) = 0.026
        _LightningSpeed ("Lightning Speed", Range(0, 12)) = 3.6
        _SparkDensity ("Spark Density", Range(5, 90)) = 36
        _SparkSpeed ("Spark Speed", Range(0, 12)) = 4.4
        _PulseSpeed ("Pulse Speed", Range(0, 12)) = 4.8

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+10"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            Name "LightningAura"

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _AuraColor;
            fixed4 _LightningColor;
            fixed4 _CoreColor;
            half _EffectAmount;
            half _Intensity;
            half _SampleScale;
            half _AlphaWeight;
            half _LuminanceWeight;
            half _MaskThreshold;
            half _GlowSize;
            half _GlowPower;
            half _InsideSuppression;
            half _LightningDensity;
            half _LightningWidth;
            half _LightningSpeed;
            half _SparkDensity;
            half _SparkSpeed;
            half _PulseSpeed;

            struct Attributes
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise21(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float2 ToSourceUV(float2 overlayUV)
            {
                return (overlayUV - 0.5) * _SampleScale + 0.5;
            }

            half InUnitSquare(float2 uv)
            {
                half2 insideMin = step(0.0, uv);
                half2 insideMax = step(uv, 1.0);
                return insideMin.x * insideMin.y * insideMax.x * insideMax.y;
            }

            half MaskAt(float2 sourceUV)
            {
                half inside = InUnitSquare(sourceUV);
                fixed4 sampleColor = tex2D(_MainTex, saturate(sourceUV)) + _TextureSampleAdd;
                half luminance = dot(sampleColor.rgb, half3(0.299, 0.587, 0.114));
                half alphaMask = smoothstep(0.01, 0.35, sampleColor.a) * _AlphaWeight;
                half luminanceMask = smoothstep(_MaskThreshold, _MaskThreshold + 0.12, luminance) * _LuminanceWeight;
                return saturate(max(alphaMask, luminanceMask) * inside);
            }

            half NeighbourMask(float2 sourceUV, float radius)
            {
                half m = 0.0;
                m = max(m, MaskAt(sourceUV + float2( radius, 0.0)));
                m = max(m, MaskAt(sourceUV + float2(-radius, 0.0)));
                m = max(m, MaskAt(sourceUV + float2(0.0,  radius)));
                m = max(m, MaskAt(sourceUV + float2(0.0, -radius)));
                m = max(m, MaskAt(sourceUV + float2( radius,  radius) * 0.7071));
                m = max(m, MaskAt(sourceUV + float2(-radius,  radius) * 0.7071));
                m = max(m, MaskAt(sourceUV + float2( radius, -radius) * 0.7071));
                m = max(m, MaskAt(sourceUV + float2(-radius, -radius) * 0.7071));
                return m;
            }

            half LightningPattern(float2 uv, float time)
            {
                float2 centered = uv - 0.5;
                float angle = atan2(centered.y, centered.x) * 0.15915494 + 0.5;
                float radius = length(centered);
                float flow = time * _LightningSpeed;

                float jag = Noise21(float2(angle * _LightningDensity * 2.3 + flow * 0.35, radius * 18.0 - flow));
                float radialStripe = abs(frac(angle * _LightningDensity + radius * 2.2 + jag * 0.72 - flow * 0.24) - 0.5);
                float radialBolt = 1.0 - smoothstep(0.0, _LightningWidth, radialStripe);

                float diagonalJag = Noise21(float2(uv.x * 9.0 - flow * 0.7, uv.y * 11.0 + flow * 0.3));
                float diagonalStripe = abs(frac((uv.x * 1.6 - uv.y * 1.15) * (_LightningDensity * 0.56) + diagonalJag * 0.9 + flow * 0.18) - 0.5);
                float diagonalBolt = 1.0 - smoothstep(0.0, _LightningWidth * 0.72, diagonalStripe);

                float flicker = 0.58 + 0.42 * Noise21(float2(floor(angle * _LightningDensity), floor(time * 15.0)));
                return saturate(max(radialBolt, diagonalBolt * 0.78) * flicker);
            }

            half SparkPattern(float2 uv, float time)
            {
                float2 gridUV = uv * _SparkDensity;
                float2 cell = floor(gridUV);
                float2 local = frac(gridUV) - 0.5;

                float rnd = Hash21(cell);
                float2 jitter = float2(Hash21(cell + 13.1), Hash21(cell + 71.7)) - 0.5;
                float life = frac(rnd + time * _SparkSpeed * (0.18 + rnd * 0.28));
                float2 drift = float2(0.08 * sin(time * 1.7 + rnd * 6.28318), life * 0.52 - 0.26);
                float distToSpark = length(local - jitter * 0.42 - drift);

                float sparkShape = 1.0 - smoothstep(0.0, 0.16, distToSpark);
                float lifeShape = smoothstep(0.0, 0.18, life) * (1.0 - smoothstep(0.34, 1.0, life));
                float gate = step(0.68, rnd);
                return sparkShape * lifeShape * gate;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                float2 sourceUV = ToSourceUV(input.uv);
                float time = _Time.y;

                half mask = MaskAt(sourceUV);
                half nearMask = NeighbourMask(sourceUV, _GlowSize);
                half midMask = NeighbourMask(sourceUV, _GlowSize * 2.35);
                half farMask = NeighbourMask(sourceUV, _GlowSize * 4.5);

                half outer = saturate(nearMask * 1.15 + midMask * 0.68 + farMask * 0.28 - mask * _InsideSuppression);
                outer = pow(saturate(outer), _GlowPower);

                half edgeBand = saturate(nearMask - mask * 0.55);
                half pulse = 0.72 + 0.28 * sin(time * _PulseSpeed + Noise21(input.uv * 6.0) * 6.28318);
                half bolt = LightningPattern(input.uv, time) * saturate(edgeBand + outer * 0.58);
                half sparks = SparkPattern(input.uv, time) * saturate(midMask + farMask - mask * 0.72);

                half effect = saturate(_EffectAmount);
                half auraAlpha = outer * 0.48 * pulse;
                half boltAlpha = bolt * 1.15;
                half sparkAlpha = sparks * 0.92;
                half alpha = saturate((auraAlpha + boltAlpha + sparkAlpha) * effect * input.color.a);

                half3 auraColor = _AuraColor.rgb * auraAlpha;
                half3 boltColor = (_LightningColor.rgb * boltAlpha * 1.9) + (_CoreColor.rgb * boltAlpha * 0.55);
                half3 sparkColor = lerp(_AuraColor.rgb, _LightningColor.rgb, 0.72) * sparkAlpha * 1.65;
                half3 color = (auraColor + boltColor + sparkColor) * _Intensity * effect * input.color.rgb;

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
