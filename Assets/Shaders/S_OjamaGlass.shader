// お邪魔ぷよのガラス玉シェーダー（不透明）
// ・ふちが青白く光る（リムライト）
// ・左上に小さな映り込み、下側にやわらかい明るさ（光が玉を通って底が明るい）
// ・斜めの光の筋が、盤面全体を左上から右下へ一定間隔で流れる
//   （ワールド座標で流すので、ぷよの位置ごとに光るタイミングが自然にずれる）
// 丸いぷよの絵（画像いっぱいに丸が描かれていて、外周に黒フチがある）を前提にしている
Shader "App/OjamaGlass"
{
    Properties
    {
        _MainTex         ("Sprite Texture", 2D)                 = "white" {}
        [Header(Body)]
        _BodyRadius      ("Body Radius (UV, inside outline)", Range(0.3, 0.5)) = 0.44
        [Header(Rim)]
        _RimColor        ("Rim Color", Color)                   = (0.55, 0.7, 0.9, 1)
        _RimStart        ("Rim Start (0-1 of radius)", Range(0, 1)) = 0.62
        _RimStrength     ("Rim Strength", Range(0, 2))          = 0.9
        [Header(Highlights)]
        _SpecStrength    ("Top-left Highlight", Range(0, 1.5))  = 0.55
        _CausticStrength ("Bottom Glow", Range(0, 1))           = 0.25
        [Header(Streak)]
        _StreakColor     ("Streak Color", Color)                = (1, 1, 1, 1)
        _StreakStrength  ("Streak Strength", Range(0, 1.5))    = 0.75
        _StreakWidth     ("Streak Width (world)", Float)        = 0.18
        _StreakSpeed     ("Streak Speed (world/sec)", Float)    = 3.5
        _StreakInterval  ("Streak Interval (world)", Float)     = 14
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
                float2 worldXY     : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float  _BodyRadius;
                float4 _RimColor;
                float  _RimStart;
                float  _RimStrength;
                float  _SpecStrength;
                float  _CausticStrength;
                float4 _StreakColor;
                float  _StreakStrength;
                float  _StreakWidth;
                float  _StreakSpeed;
                float  _StreakInterval;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                OUT.worldXY     = TransformObjectToWorld(IN.positionOS.xyz).xy;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // 玉の中心からの距離（0=中心、1=黒フチの内側）
                float2 p    = IN.uv - 0.5;
                float  r    = length(p) / _BodyRadius;
                float  body = 1.0 - smoothstep(0.97, 1.0, r); // フチより内側だけに効果をかける

                // ふちの光
                float rim = smoothstep(_RimStart, 1.0, r);
                col.rgb  += _RimColor.rgb * rim * rim * _RimStrength * body;

                // 左上の小さな映り込み（楕円）
                float2 sp   = (p - float2(-0.17, 0.2)) / float2(0.12, 0.07);
                float  spec = saturate(1.0 - dot(sp, sp));
                col.rgb    += spec * spec * _SpecStrength * body;

                // 下側のやわらかい明るさ
                float2 cp      = (p - float2(0.0, -0.27)) / float2(0.27, 0.11);
                float  caustic = saturate(1.0 - length(cp));
                col.rgb       += caustic * _CausticStrength * body;

                // 斜めの光の筋（左上→右下へ流れる帯。ワールド座標なので盤面全体で1本の帯になる）
                float s      = (IN.worldXY.x - IN.worldXY.y) * 0.7071;
                float band   = frac((s - _Time.y * _StreakSpeed) / _StreakInterval) * _StreakInterval;
                float dist   = band - _StreakInterval * 0.5;
                float streak = exp(-(dist * dist) / (_StreakWidth * _StreakWidth))
                             + 0.5 * exp(-((dist + _StreakWidth * 2.2) * (dist + _StreakWidth * 2.2)) / (_StreakWidth * _StreakWidth * 0.2));
                col.rgb     += _StreakColor.rgb * streak * _StreakStrength * body;

                return col * IN.color;
            }
            ENDHLSL
        }
    }
}
