#nullable enable
using UnityEngine;

namespace App.Puyo
{
    /// <summary>
    /// 岩化されたぷよの耐久状態。
    /// 敵スキル「岩」により既存の PuyoPiece へ後付けでアタッチされる。
    /// 消去に巻き込まれても耐久が残っていれば実際には消えず、1回目のヒットでスプライトが変わる。
    /// </summary>
    [RequireComponent(typeof(PuyoPiece))]
    public sealed class RockPuyoState : MonoBehaviour
    {
        private PuyoPiece? _piece;
        private Sprite?    _hitSprite;
        private int        _durabilityRemaining;
        private bool       _hasHitOnce;

        private void Awake() => _piece = GetComponent<PuyoPiece>();

        /// <summary>耐久値と、1回目ヒット時に差し替えるスプライトを設定する</summary>
        public void Initialize(int durability, Sprite? hitSprite)
        {
            _durabilityRemaining = Mathf.Max(1, durability);
            _hitSprite           = hitSprite;
        }

        /// <summary>
        /// 消去に巻き込まれた際に PuyoBoard から呼ぶ。
        /// 耐久が残っていれば見た目を変えて true を返す（＝呼び出し元は実際には消さないこと）。
        /// 耐久が尽きていれば false を返す（＝通常通り消してよい）。
        /// </summary>
        public bool OnHit()
        {
            _durabilityRemaining--;
            if (_durabilityRemaining <= 0) return false;

            if (!_hasHitOnce)
            {
                // ヒット後の画像があれば差し替えて本来の色で見せ、未設定ならさらに暗い灰色にしてヒットしたことを示す
                if (_hitSprite != null) _piece?.SetSprite(_hitSprite);
                _piece?.SetTint(HitTintFor(_hitSprite));
                _hasHitOnce = true;
            }
            return true;
        }

        /// <summary>
        /// 1回目のヒット後に本体へかける色。
        /// 専用画像があるなら岩化時の灰色の乗算を外し（白）、なければ暗い灰色にする。
        /// </summary>
        public static Color HitTintFor(Sprite? hitSprite)
            => hitSprite != null ? Color.white : FallbackSkillVisuals.RockHit;
    }
}
#nullable disable
