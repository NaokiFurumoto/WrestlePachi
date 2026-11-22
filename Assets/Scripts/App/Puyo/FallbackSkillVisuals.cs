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
