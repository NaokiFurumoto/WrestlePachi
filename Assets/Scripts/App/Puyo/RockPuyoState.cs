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
                // ヒット後の画像があれば差し替え、未設定ならさらに暗い灰色にしてヒットしたことを示す
                if (_hitSprite != null) _piece?.SetSprite(_hitSprite);
                else                    _piece?.SetTint(FallbackSkillVisuals.RockHit);
                _hasHitOnce = true;
            }
            return true;
        }
    }
}
#nullable disable
