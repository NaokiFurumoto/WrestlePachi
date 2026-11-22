#nullable enable
using UnityEngine;

namespace App.Puyo
{
    /// <summary>
    /// 盤面用の画像がまだない敵スキル（炎マス・時限爆弾・岩）の仮表示。
    /// EnemySkillAssets にスプライトが設定されていればそちらが優先され、未設定のときだけ使う。
    /// </summary>
    public static class FallbackSkillVisuals
    {
        /// <summary>炎マスの四角の色（半透明オレンジ）</summary>
        public static readonly Color FireTile = new(1f, 0.478f, 0.102f, 0.45f);

        /// <summary>時限爆弾ぷよの色（お邪魔ぷよを赤く染める）</summary>
        public static readonly Color Bomb = new(1f, 0.251f, 0.251f, 1f);

        /// <summary>岩化したぷよの色（灰色）</summary>
        public static readonly Color Rock = new(0.549f, 0.549f, 0.549f, 1f);

        /// <summary>1回目のヒット後の岩の色（さらに暗い灰色）</summary>
        public static readonly Color RockHit = new(0.35f, 0.35f, 0.35f, 1f);

        private static Sprite? _softCircle;

        /// <summary>
        /// 縁がやわらかい白い丸（1ワールド単位）。サメの泡・焦げる煙などに使う。初回だけ生成して使い回す。
        /// </summary>
        public static Sprite SoftCircleSprite
        {
            get
            {
                if (_softCircle != null) return _softCircle;
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var center = (size - 1) * 0.5f;
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var d     = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    var alpha = Mathf.Clamp01((1f - d) / 0.25f); // 外側25%で徐々に透明に
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                tex.Apply();
                _softCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                _softCircle.name = "SoftCircle";
                return _softCircle;
            }
        }

        private static Sprite? _squareSprite;

        /// <summary>
        /// 1ワールド単位の白い正方形スプライト（炎マスの仮表示用）。
        /// 初回アクセス時に1回だけ生成して使い回す。
        /// </summary>
        public static Sprite SquareSprite
        {
            get
            {
                if (_squareSprite != null) return _squareSprite;
                var tex = Texture2D.whiteTexture;
                _squareSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                              new Vector2(0.5f, 0.5f), tex.width);
                _squareSprite.name = "FallbackSquare";
                return _squareSprite;
            }
        }
    }
}
#nullable disable
