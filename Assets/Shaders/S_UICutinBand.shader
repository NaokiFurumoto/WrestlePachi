Shader "WrestlePachi/UI/CutinBand"
{
    Properties
    {
        [PerRendererData] _MainTex ("Source Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Base Band)]
        [HDR] _BaseColor ("Base Color", Color) = (0.02, 0.04, 0.15, 0.95)

        [Header(Inner Edge Glow)]
        [HDR] _GlowColor ("Glow Color", Color) = (1.0, 0.55, 0.08, 1)
        _GlowWidth     ("Glow Width",     Range(0.5, 8))  = 2.5
        _GlowIntensity ("Glow Intensity", Range(0, 6))    = 2.5
        // 0 = 下辺が内側（CitinLineTop 用）  1 = 上辺が内側（CitinLineUnder 用）
        _InnerEdge     ("Inner Edge  0=Bottom  1=Top", Range(0, 1)) = 0

        [Header(Energy Lines)]
        [HDR] _LineColor    ("Line Color",      Color)          = (1.2, 1.0, 0.45, 1)
        _LineSpeed          ("Line Speed",      Range(1, 120))  = 50
        _LineCount          ("Line Count",      Range(2, 8))    = 7
        _LineWidth          ("Line Width",      Range(0.002, 0.1)) = 0.022
        _LineBrightness     ("Line Brightness", Range(0, 6))    = 3.0
        _TailLength         ("Tail Length",     Range(0.05, 0.8)) = 0.35

        [Header(Particles)]
        [HDR] _SparkColor   ("Spark Color",      Color)         = (1.0, 1.0, 0.75, 1)
        _SparkDensity       ("Spark Density",    Range(5, 80))  = 28
        _SparkSpeed         ("Spark Speed",      Range(1, 60))  = 18
        _SparkBrightness    ("Spark Brightness", Range(0, 5))   = 2.0

        [Header(Master)]
        _Intensity ("Intensity", Range(0, 10)) = 3.5

        [HideInInspector] _StencilComp      ("Stencil Comparison",  Float) = 8
        [HideInInspector] _Stencil          ("Stencil ID",          Float) = 0
        [HideInInspector] _StencilOp        ("Stencil Operation",   Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask",  Float) = 255
        [HideInInspector] _StencilReadMask  ("Stencil Read Mask",   Float) = 255
        [HideInInspector] _ColorMask        ("Color Mask",          Float) = 15
        [HideInInspector] _UseUIAlphaClip   ("Use Alpha Clip",      Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent+10"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref        [_Stencil]
            Comp       [_StencilComp]
            Pass       [_StencilOp]
            ReadMask   [_StencilReadMask]
            WriteMask  [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "CutinBand"

            CGPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            fixed4    _TextureSampleAdd;
            float4    _ClipRect;

            fixed4 _BaseColor;
            fixed4 _GlowColor;
            half   _GlowWidth;
            half   _GlowIntensity;
            half   _InnerEdge;
            fixed4 _LineColor;
            half   _LineSpeed;
            half   _LineCount;
            half   _LineWidth;
            half   _LineBrightness;
            half   _TailLength;
            fixed4 _SparkColor;
            half   _SparkDensity;
            half   _SparkSpeed;
            half   _SparkBrightness;
            half   _Intensity;

            struct Attributes
            {
                float4 vertex   : POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ─── ユーティリティ ──────────────────────────────────────────────

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                return frac(p * (p + p));
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            float Noise21(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // ─── エッジグロー ───────────────────────────────────────────────
            // _InnerEdge=0: UV.y=0 が内側（CitinLineTop：下辺が画面中央向き）
            // _InnerEdge=1: UV.y=1 が内側（CitinLineUnder：上辺が画面中央向き）
            half ComputeEdgeGlow(float2 uv, float time)
            {
                // 0=内側エッジ / 1=外側エッジ
                float innerDist = lerp(uv.y, 1.0 - uv.y, _InnerEdge);

                // 指数的グロー：内側ほど強く、外側へ急減衰
                half glow = pow(saturate(1.0 - innerDist), _GlowWidth) * _GlowIntensity;

                // 脈動：時間と位置ノイズで揺らす
                half pulse = 0.8 + 0.2 * sin(time * 6.3 + Noise21(float2(uv.x * 4.0, time * 0.4)) * 6.283);

                // 内側エッジ沿いの細かいシマー（不均一な発光感）
                half shimmer = Noise21(float2(uv.x * 14.0 - time * 4.0, innerDist * 5.0)) * 0.3;
                half shimMask = saturate(1.0 - innerDist * 2.8);

                return glow * pulse + shimmer * shimMask;
            }

            // ─── エネルギーストリーク ────────────────────────────────────────
            // 高速で左→右へ横断する光の尾を引いたライン群
            half ComputeEnergyLines(float2 uv, float time)
            {
                float result = 0.0;

                // 最大 8 本分ループ（_LineCount でアクティブ本数制御）
                for (int i = 0; i < 8; i++)
                {
                    float fi     = float(i);
                    float active = step(fi, _LineCount - 1.0); // 非アクティブ本は結果に加算されない

                    float rnd  = Hash11(fi * 3.71 + 1.23);
                    float rnd2 = Hash11(fi * 7.13 + 4.56);

                    // Y 位置（帯内に均等分散 + 微細な時間揺れ）
                    float baseY  = 0.08 + fi / 8.0 * 0.84;
                    float wiggle = Noise21(float2(fi * 5.3, time * (0.5 + rnd * 0.5))) * 0.05 - 0.025;
                    float lineY  = saturate(baseY + wiggle);

                    // 先端位置（0→1 を一定速度でスクロール）
                    float speed = _LineSpeed * (0.55 + rnd * 0.9) * 0.01;
                    float pos   = frac(time * speed + rnd2);

                    // 先端からの相対距離（0=先端, 正値=尾方向）
                    float xFromHead = frac(pos - uv.x + 1.0);

                    // テール範囲内で指数減衰する尾
                    float inTail = step(xFromHead, _TailLength);
                    float tail   = inTail * exp(-xFromHead * (5.5 / _TailLength));

                    // 先端を特に明るくブースト
                    float headBoost = exp(-xFromHead * 90.0) * 3.5;

                    // Y 方向マスク（線の細さ）
                    float yDist = abs(uv.y - lineY);
                    float yMask = 1.0 - smoothstep(0.0, _LineWidth, yDist);

                    result += (tail + headBoost) * yMask * (0.4 + rnd * 0.7) * active;
                }

                return saturate(result);
            }

            // ─── スパーク粒子 ────────────────────────────────────────────────
            // ストリームに乗って右へ流れる微小発光粒子
            half ComputeSparks(float2 uv, float time)
            {
                // X 方向に細かいグリッド
                float2 grid  = uv * float2(_SparkDensity * 2.8, _SparkDensity);
                float2 cell  = floor(grid);
                float2 local = frac(grid) - 0.5;

                float rnd  = Hash21(cell);
                float rnd2 = Hash21(cell + float2(31.7, 17.3));

                // X 方向へ流れる（速度にランダム差）
                float speed  = _SparkSpeed * (0.5 + rnd * 0.8) * 0.01;
                float life   = frac(rnd + time * speed * 2.5);
                float2 drift = float2(-life * 0.85, 0.0);
                float2 jitter = float2(rnd - 0.5, rnd2 - 0.5) * 0.4;

                float dist  = length(local - jitter - drift);
                float size  = 0.05 + rnd2 * 0.06;
                float shape = 1.0 - smoothstep(0.0, size, dist);

                // 生まれて消えるライフサイクル
                float fade = smoothstep(0.0, 0.12, life) * (1.0 - smoothstep(0.55, 1.0, life));

                // セルの半数にのみ出現（過密を防ぐ）
                float gate = step(0.48, rnd);

                return shape * fade * gate * (0.5 + rnd * 1.0);
            }

            // ─── 背景シマー（帯全体のエネルギー感） ─────────────────────────
            half ComputeShimmer(float2 uv, float time)
            {
                float n1 = Noise21(float2(uv.x * 7.0  - time * 5.0, uv.y * 3.0));
                float n2 = Noise21(float2(uv.x * 20.0 - time * 11.0, uv.y * 8.0 + 3.5));
                // 内側エッジ付近だけシマーを強くする
                float innerDist = lerp(uv.y, 1.0 - uv.y, _InnerEdge);
                float edgeMask  = saturate(1.0 - innerDist * 2.5);
                return (n1 * 0.65 + n2 * 0.35) * 0.22 * edgeMask;
            }

            // ─── 頂点シェーダー ──────────────────────────────────────────────
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex        = UnityObjectToClipPos(output.worldPosition);
                output.uv            = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color         = input.color * _Color;
                return output;
            }

            // ─── フラグメントシェーダー ──────────────────────────────────────
            fixed4 Frag(Varyings input) : SV_Target
            {
                float2 uv  = input.uv;
                float  time = _Time.y;

                // ── ベース帯（MainTex × BaseColor） ──
                fixed4 texSample = tex2D(_MainTex, uv) + _TextureSampleAdd;
                half4  base      = _BaseColor;
                base.rgb *= texSample.rgb;
                base.a   *= texSample.a * input.color.a;

                // ── 各エフェクト計算 ──
                half edgeGlow = ComputeEdgeGlow(uv, time);
                half lines    = ComputeEnergyLines(uv, time);
                half sparks   = ComputeSparks(uv, time);
                half shimmer  = ComputeShimmer(uv, time);

                half3 glowCol  = _GlowColor.rgb * edgeGlow;
                half3 lineCol  = _LineColor.rgb  * lines   * _LineBrightness;
                half3 sparkCol = _SparkColor.rgb * sparks  * _SparkBrightness;
                half3 shimCol  = _GlowColor.rgb  * shimmer; // シマーはグロー色で統一

                // ── 合成（ベース暗色 + 加算的エフェクト） ──
                half3 effects  = (glowCol + lineCol + sparkCol + shimCol) * _Intensity * input.color.rgb;
                half3 finalRgb = base.rgb + effects;
                half  finalA   = base.a;

                #ifdef UNITY_UI_CLIP_RECT
                finalA *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalA - 0.001);
                #endif

                return fixed4(finalRgb, finalA);
            }
            ENDCG
        }
    }
}
