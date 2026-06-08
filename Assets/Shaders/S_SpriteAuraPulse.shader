// ============================================================
//  S_SpriteAuraPulse.shader
//  2D SpriteRenderer 用・パルスオーラエフェクト
//
//  【セットアップ手順】
//  1. SpriteRenderer（キャラクター本体）の下に空 GameObject を配置
//  2. そこに SpriteRenderer を追加し、同じスプライトをアサイン
//  3. このマテリアル（M_SpriteAuraPulse）をアサイン
//  4. _SampleScale を調整してオーラ幅を確保（デフォルト 1.3）
//     ※ テクスチャの Import Settings で Wrap Mode = Clamp にすること
//
//  【ブレンド】Blend SrcAlpha One（加算）→ 重なるほど明るく発光
// ============================================================
Shader "WrestlePachi/SpriteAuraPulse"
{
    Properties
    {
        [PerRendererData] _MainTex ("スプライトテクスチャ", 2D) = "white" {}

        [Header(Aura)]
        _AuraColor      ("オーラカラー",           Color)             = (0.4, 0.1, 1.0, 1.0)
        _GlowSize       ("グロー幅 (UV単位)",       Range(0.01, 0.12)) = 0.04
        _GlowIntensity  ("発光強度",               Range(0.5, 8.0))   = 3.5
        _SampleScale    ("サンプルスケール",        Range(1.0, 1.8))   = 1.3
        _AlphaThreshold ("アルファ閾値",            Range(0.01, 0.5))  = 0.1

        [Header(Pulse)]
        _PulseSpeed     ("パルス速度",              Range(0.3, 4.0))   = 1.0
        _WaveCount      ("波の数 (1〜4)",            Range(1, 4))       = 3

        [Header(Noise)]
        _NoiseScale     ("ノイズスケール",           Range(1, 20))      = 6.0
        _NoiseSpeed     ("ノイズ速度",              Range(0, 3))       = 0.4
        _NoiseStrength  ("ノイズ強度",              Range(0, 1))       = 0.35

        [Header(Rainbow)]
        [Toggle(_RAINBOW_ON)] _RainbowMode ("虹色モード", Float) = 0
        _RainbowSpeed   ("虹色サイクル速度",        Range(0.1, 3.0))   = 0.5
        _Saturation     ("彩度",                   Range(0, 1))       = 1.0
        _Brightness     ("明度",                   Range(0.5, 4))     = 1.8
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+1"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
        }

        Blend  SrcAlpha One  // 加算合成 → 輝くような発光表現
        Cull   Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.0
            #pragma multi_compile_local _ _RAINBOW_ON

            #include "UnityCG.cginc"

            // ── ユニフォーム ────────────────────────────────────────────────
            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _AuraColor;
            half      _GlowSize;
            half      _GlowIntensity;
            half      _SampleScale;
            half      _AlphaThreshold;
            half      _PulseSpeed;
            half      _WaveCount;
            half      _NoiseScale;
            half      _NoiseSpeed;
            half      _NoiseStrength;
            half      _RainbowMode;
            half      _RainbowSpeed;
            half      _Saturation;
            half      _Brightness;

            // ── 頂点 I/O ────────────────────────────────────────────────────
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            // ── Voronoi ノイズ ───────────────────────────────────────────────
            // セルラーノイズ。生命感のある有機的な揺らぎを作る

            float Hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 74.31);
                return frac(p.x * p.y);
            }

            float Voronoi(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float  minD = 8.0;

                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 nb  = float2(x, y);
                    float2 rng = float2(Hash21(i + nb + 0.1), Hash21(i + nb + 73.3));
                    // セル中心点を時間でアニメーション
                    float2 pt  = nb + 0.5 + 0.45 * sin(6.28318 * rng + _Time.y * _NoiseSpeed);
                    float2 d   = pt - f;
                    minD = min(minD, dot(d, d));
                }
                return sqrt(minD);
            }

            // ── シルエット検出 ──────────────────────────────────────────────
            // _SampleScale > 1 にすることで、スプライトが UV 空間に小さく配置される。
            // はみ出た領域（透明パディング）がオーラを描画するスペースになる。

            float2 ToSourceUV(float2 uv)
            {
                return (uv - 0.5) * _SampleScale + 0.5;
            }

            // アルファからマスク値を取得（UV 範囲外 → 0）
            half MaskAt(float2 uv)
            {
                half inside = step(0.0, uv.x) * step(uv.x, 1.0)
                            * step(0.0, uv.y) * step(uv.y, 1.0);
                return smoothstep(_AlphaThreshold, _AlphaThreshold + 0.1,
                                  tex2D(_MainTex, saturate(uv)).a) * inside;
            }

            // 8方向サンプリングでシルエットを radius だけ膨張させたマスク
            half NeighbourMask(float2 uv, float radius)
            {
                float r = radius;
                float d = radius * 0.7071; // 斜め方向補正
                half  m = 0.0;
                m = max(m, MaskAt(uv + float2( r,  0)));
                m = max(m, MaskAt(uv + float2(-r,  0)));
                m = max(m, MaskAt(uv + float2( 0,  r)));
                m = max(m, MaskAt(uv + float2( 0, -r)));
                m = max(m, MaskAt(uv + float2( d,  d)));
                m = max(m, MaskAt(uv + float2(-d,  d)));
                m = max(m, MaskAt(uv + float2( d, -d)));
                m = max(m, MaskAt(uv + float2(-d, -d)));
                return m;
            }

            // ── HSV → RGB ───────────────────────────────────────────────────
            half3 HsvToRgb(half h, half s, half v)
            {
                half3 rgb = saturate(abs(frac(h + half3(0.0, 0.667, 0.333)) * 6.0 - 3.0) - 1.0);
                return v * lerp(half3(1, 1, 1), rgb, s);
            }

            // ── フラグメントシェーダー ─────────────────────────────────────
            fixed4 Frag(Varyings i) : SV_Target
            {
                float2 srcUV = ToSourceUV(i.uv);
                float  t     = _Time.y;

                // スプライト本体マスク（ここはオーラを描画しない）
                half mask = MaskAt(srcUV);

                // 3段階の半径でシルエット周辺を検出
                half r1 = NeighbourMask(srcUV, _GlowSize);         // 最近傍
                half r2 = NeighbourMask(srcUV, _GlowSize * 2.5);   // 中間
                half r3 = NeighbourMask(srcUV, _GlowSize * 5.0);   // 遠方

                // オーラ領域：スプライト外周のみ描画
                half auraZone = r3 - mask;
                if (auraZone <= 0.01) return fixed4(0, 0, 0, 0);

                // シルエット境界からの正規化距離（0=境界直近, 1=外縁）
                // 3段階のマスクを重み付き合成で近似する
                half dist = saturate(1.0 - r1 * 0.60 - r2 * 0.25 - r3 * 0.15);

                // ── Voronoi ノイズで輪郭を有機的に歪める ─────────────────
                float2 noiseUV = i.uv * _NoiseScale + float2(t * 0.13, t * 0.07);
                float  noise   = Voronoi(noiseUV) * 0.9;         // 0〜0.9
                float  distN   = saturate(dist + (noise - 0.42) * _NoiseStrength);

                // ── 複数パルス波 ──────────────────────────────────────────
                // 波頭は dist=0（シルエット境界）から dist=1（外縁）へ向かって拡大する
                float aura = 0.0;

                for (int w = 0; w < 4; w++)
                {
                    // _WaveCount で有効な波数を制御（step でマスク）
                    float enable    = step((float)w, _WaveCount - 1.0);
                    // 各波の位相オフセット（均等分割）
                    float phase     = (float)w * 0.25;
                    float waveFront = frac(t * _PulseSpeed * 0.7 + phase);
                    // 波頭付近でガウス状に輝き、拡大とともに減衰
                    float waveDist  = abs(distN - waveFront);
                    float wave      = exp(-waveDist * 16.0) * (1.0 - waveFront) * 1.6;
                    aura += wave * enable;
                }

                // ── ベース発光（常駐するグロー）──────────────────────────
                // シルエット境界に常に輝く光の輪
                float baseGlow = pow(max(0.0, 1.0 - distN * 1.6), 2.8) * 0.9;

                // ── 呼吸パルス（全体の周期的な強弱）─────────────────────
                // ノイズで変調することで機械的な周期感を和らげる
                float breathe = 0.6 + 0.4 * sin(t * _PulseSpeed * 3.14159 + noise * 2.8);

                aura = saturate((aura + baseGlow) * breathe) * _GlowIntensity;

                // ── 色 ───────────────────────────────────────────────────
                half3 col;

                #ifdef _RAINBOW_ON
                    // 時間・距離・ノイズで色相をずらすことで生きた虹色に
                    float hue = frac(t * _RainbowSpeed * 0.12 - distN * 0.4 + noise * 0.25);
                    col = HsvToRgb(hue, _Saturation, _Brightness);
                #else
                    col = _AuraColor.rgb;
                #endif

                // 外縁フェードアウト
                float outerFade = (1.0 - distN * 0.9) * auraZone;
                float alpha     = saturate(aura * outerFade);

                return fixed4(col * alpha, alpha) * i.color;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
