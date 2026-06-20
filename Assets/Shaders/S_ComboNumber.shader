// TMP WorldSpace 用 カスタム SDF シェーダー
// ・縦グラデーション Fill（黄→オレンジ）
// ・ゴールド 2層アウトライン（外=濃茶金, 内=明るい金）
// ・上部グロスハイライト
//
// 使い方：TextMeshPro コンポーネントの Material に
//         このシェーダーを使った Material を指定する。
// GradientYMin / GradientYMax は文字の local Y 範囲に合わせて調整。
// （例：FontSize=5 なら ±0.35 程度）

Shader "App/ComboNumber"
{
    Properties
    {
        [HideInInspector] _MainTex ("Font Atlas", 2D) = "white" {}

        [Header(Fill Gradient)]
        _FaceColorTop    ("Top Color",    Color) = (1.00, 0.96, 0.25, 1)
        _FaceColorBottom ("Bottom Color", Color) = (1.00, 0.40, 0.04, 1)
        _GradientYMin    ("Gradient Y Min", Float) = -0.45
        _GradientYMax    ("Gradient Y Max", Float) =  0.45

        [Header(Gold Outline)]
        _OutlineColorOuter ("Outer Edge",  Color)        = (0.38, 0.22, 0.00, 1)
        _OutlineColorInner ("Inner Edge",  Color)        = (0.98, 0.78, 0.08, 1)
        _OutlineWidth      ("Width",       Range(0, 0.5)) = 0.24

        [Header(Gloss Highlight)]
        _GlossColor    ("Color",    Color)           = (1.00, 1.00, 1.00, 0.55)
        _GlossCenter   ("Center Y", Range(0, 1))     = 0.72
        _GlossSoftness ("Softness", Range(0.01, 0.5)) = 0.18

        [Header(SDF)]
        _GradientScale ("Gradient Scale", Float)     = 5.0
        _Softness      ("Min Softness",   Range(0, 0.5)) = 0.04

        [HideInInspector] _TextureSampleAdd ("", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _FaceColorTop;
                float4 _FaceColorBottom;
                float  _GradientYMin;
                float  _GradientYMax;
                float4 _OutlineColorOuter;
                float4 _OutlineColorInner;
                float  _OutlineWidth;
                float4 _GlossColor;
                float  _GlossCenter;
                float  _GlossSoftness;
                float  _GradientScale;
                float  _Softness;
                float4 _TextureSampleAdd;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                float  localY     : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color      = IN.color;
                OUT.localY     = IN.positionOS.y;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half d = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a
                       + _TextureSampleAdd.a;

                // スクリーン解像度に応じた自動 softness（TMP標準方式）
                half autoSoft = fwidth(d) * _GradientScale;
                half s = max(autoSoft, (half)_Softness);

                // ── マスク ───────────────────────────────────────
                half fillMask  = smoothstep(0.5h - s, 0.5h + s, d);
                half outEdge   = 0.5h - (half)_OutlineWidth;
                half totalMask = smoothstep(outEdge - s, outEdge + s, d);

                // ── 縦グラデーション t: 0=下, 1=上 ───────────────
                half gradT = saturate(
                    (IN.localY - _GradientYMin)
                    / max(0.001, _GradientYMax - _GradientYMin));

                // ── Fill ─────────────────────────────────────────
                half4 faceCol = lerp(_FaceColorBottom, _FaceColorTop, gradT);
                faceCol *= IN.color;   // TMP vertex color（alpha フェードに使用）

                // ── Outline：外周ほど暗い金 ───────────────────────
                half outlineT   = saturate((d - outEdge) / max(0.001h, 0.5h - outEdge));
                half4 outlineCol = lerp(_OutlineColorOuter, _OutlineColorInner, outlineT);

                // ── Gloss：文字上部の白ハイライト ─────────────────
                half glossMask = (1.0h - smoothstep(
                    (half)_GlossCenter - (half)_GlossSoftness,
                    (half)_GlossCenter + (half)_GlossSoftness,
                    gradT)) * fillMask;

                // ── 合成 ─────────────────────────────────────────
                half4 col  = lerp(outlineCol, faceCol, fillMask);
                col.rgb   += _GlossColor.rgb * (_GlossColor.a * glossMask);
                col.a      = totalMask * IN.color.a;   // IN.color.a でフェード制御

                return col;
            }
            ENDHLSL
        }
    }
}
