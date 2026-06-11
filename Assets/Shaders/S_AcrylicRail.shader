Shader "WrestlePachi/S_AcrylicRail"
{
    // アクリル・クリアプラスチック質感シェーダー（2D Sprite 対応版）
    // SpriteRenderer にアタッチして使用する。
    // 法線の代わりに UV 端検出でエッジを光らせ、アクリル感を演出する。
    Properties
    {
        // SpriteRenderer がスプライトテクスチャをここに差し込む
        [PerRendererData] _MainTex ("スプライトテクスチャ", 2D) = "white" {}

        [Header(Base)]
        _Color ("ベースカラー (RGBA)", Color) = (0.82, 0.92, 1.0, 0.28)

        [Header(Edge Glow)]
        _EdgeWidth     ("エッジ幅",    Range(0.01, 0.5)) = 0.1
        _EdgeColor     ("エッジカラー", Color)           = (0.75, 0.95, 1.0, 1.0)
        _EdgeIntensity ("エッジ輝度",  Range(0.0, 3.0)) = 1.5

        [Header(Glare)]
        _GlareColor     ("グレア カラー",  Color)           = (1, 1, 1, 1)
        _GlarePosY      ("グレア Y位置",   Range(0.0, 1.0)) = 0.3
        _GlareWidth     ("グレア 幅",      Range(0.005, 0.5)) = 0.08
        _GlareIntensity ("グレア 輝度",    Range(0.0, 2.0)) = 0.8

        [Header(Shimmer)]
        _ShimmerSpeed     ("シマー 速度",   Range(0.0, 4.0)) = 0.8
        _ShimmerIntensity ("シマー 輝度",   Range(0.0, 1.0)) = 0.25
        _ShimmerColor     ("シマー カラー", Color)           = (1.0, 1.0, 1.0, 1.0)

        [Header(Scratch)]
        _ScratchTex       ("傷テクスチャ",  2D)              = "white" {}
        _ScratchIntensity ("傷 強度",       Range(0.0, 1.0)) = 0.15

        // SpriteRenderer 内部で使用するプロパティ（非表示）
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [PerRendererData] [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull   Off
        ZWrite Off
        Blend  SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "AcrylicRailSprite"

            CGPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   2.0
            #pragma multi_compile_local _ PIXELSNAP_ON

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;

            fixed4 _Color;

            half   _EdgeWidth;
            fixed4 _EdgeColor;
            half   _EdgeIntensity;

            fixed4 _GlareColor;
            half   _GlarePosY;
            half   _GlareWidth;
            half   _GlareIntensity;

            half   _ShimmerSpeed;
            half   _ShimmerIntensity;
            fixed4 _ShimmerColor;

            sampler2D _ScratchTex;
            float4    _ScratchTex_ST;
            half      _ScratchIntensity;

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct Varyings
            {
                float4 pos   : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv    : TEXCOORD0;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;

                #ifdef PIXELSNAP_ON
                o.pos = UnityPixelSnap(o.pos);
                #endif

                return o;
            }

            fixed4 Frag(Varyings i) : SV_Target
            {
                float2 uv   = i.uv;
                float  time = _Time.y;

                // スプライトのアルファ形状を取得（切り抜き用）
                fixed4 spriteTex = tex2D(_MainTex, uv);

                // ── UV エッジ検出（フレネル代替）─────────────────────
                // UV 端（0/1 付近）を光らせてアクリルの輪郭感を出す
                float edgeX    = min(uv.x, 1.0 - uv.x);
                float edgeY    = min(uv.y, 1.0 - uv.y);
                float edgeDist = min(edgeX, edgeY);
                float edge     = pow(1.0 - saturate(edgeDist / _EdgeWidth), 1.5);

                // ── グレア（Y 軸固定のハイライト帯）─────────────────
                // アクリル表面に走る横方向の光の映り込みを模倣する
                float glareDist = abs(uv.y - _GlarePosY);
                float glare     = pow(saturate(1.0 - glareDist / _GlareWidth), 2.0);

                // ── シマー（左→右に流れる細い光の筋）────────────────
                float shimmerPhase = uv.x - time * _ShimmerSpeed;
                half  shimmer      = pow(max(0.0, 1.0 - abs(frac(shimmerPhase) - 0.5) * 6.0), 3.0);

                // ── 傷テクスチャ（スクラッチ感）──────────────────────
                float2 scratchUV  = TRANSFORM_TEX(uv, _ScratchTex);
                half   scratchVal = tex2D(_ScratchTex, scratchUV).r;

                // ── 合成 ─────────────────────────────────────────────
                half3 col  = i.color.rgb;
                col += edge    * _EdgeColor.rgb    * _EdgeIntensity;
                col += glare   * _GlareColor.rgb   * _GlareIntensity;
                col += shimmer * _ShimmerColor.rgb * _ShimmerIntensity;
                col += scratchVal                  * _ScratchIntensity;

                // エッジ部分でアルファを上げてアクリルの厚み感を演出
                half alpha = saturate(i.color.a + edge * 0.5h);

                // スプライトの形状（アルファ）で最終的に切り抜く
                alpha *= spriteTex.a;

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
