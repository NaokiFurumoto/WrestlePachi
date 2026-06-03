Shader "WrestlePachi/HoldBallAura"
{
    Properties
    {
        _MainTex      ("Sprite Texture",  2D)           = "white" {}
        _AuraColor    ("Aura Color",      Color)        = (1, 0.8, 0, 1)
        _Intensity    ("Intensity",       Range(0, 10)) = 4.0
        _RayCount     ("Ray Count",       Range(2, 8))  = 4
        _RaySharpness ("Ray Sharpness",   Range(1, 60)) = 25
        _RingRadius   ("Ring Radius",     Range(0, 1))  = 0.65
        _RingWidth    ("Ring Width",      Range(1, 60)) = 18
        _PulseSpeed   ("Pulse Speed",     Range(0, 10)) = 2.5
        _PulseMin     ("Pulse Min",       Range(0, 1))  = 0.35
        _RotateSpeed  ("Rotate Speed",    Range(-5, 5)) = 0.4
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
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One  // Additive — 輝くような発光

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _AuraColor;
            float     _Intensity;
            float     _RayCount;
            float     _RaySharpness;
            float     _RingRadius;
            float     _RingWidth;
            float     _PulseSpeed;
            float     _PulseMin;
            float     _RotateSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // スプライトの中心を原点に（-1〜1）
                float2 uv   = i.uv * 2.0 - 1.0;
                float  dist = length(uv);

                // 回転アニメーション
                float angle = atan2(uv.y, uv.x) + _Time.y * _RotateSpeed;

                // ── 放射状レイ ──────────────────────────────────────
                // cos(angle * N) を鋭く絞ることで N 本の光線を生成
                float rays  = pow(max(0.0, cos(angle * _RayCount)),       _RaySharpness);
                // 45度回転した補助レイ（交差する線を増やす）
                float rays2 = pow(max(0.0, cos(angle * _RayCount + 0.785)), _RaySharpness * 0.6);

                // ── 外周リング ──────────────────────────────────────
                float ring = exp(-pow((dist - _RingRadius) * _RingWidth, 2.0));

                // ── 中心グロー ──────────────────────────────────────
                float center = exp(-dist * 5.0);

                // ── 外周に向かってフェード ───────────────────────────
                float radialFade = saturate(1.8 - dist * 1.8);

                // 各要素を合成
                float glow = (rays * 0.7 + rays2 * 0.4 + ring * 0.9 + center * 0.6)
                             * radialFade;

                // ── パルスアニメーション ─────────────────────────────
                float pulse = lerp(_PulseMin, 1.0,
                              (sin(_Time.y * _PulseSpeed) + 1.0) * 0.5);

                float alpha = saturate(glow * pulse);
                return fixed4(_AuraColor.rgb * _Intensity * pulse, alpha) * i.color;
            }
            ENDCG
        }
    }
}
