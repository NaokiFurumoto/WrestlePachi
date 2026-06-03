#nullable enable
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 保留玉の表示を管理する。
    /// HoldSystem のイベントを受け取り、スロット位置に HoldBall を生成・破棄する。
    /// </summary>
    public sealed class HoldDisplay : MonoBehaviour
    {
        // ─── Inspector 参照 ──────────────────────────────────────
        [Header("保留スロット（HoldSystem.MaxHolds 個の配置位置）")]
        [SerializeField] private Transform[] _slots = null!;

        [Header("HoldBall Prefab")]
        [SerializeField] private HoldBall _prefab = null!;

        [Header("スプライト（HoldType の int 値順: Red/Yellow/Green/Blue/Black）")]
        [SerializeField] private Sprite[] _sprites = null!;

        // ─── 状態 ───────────────────────────────────────────────
        private readonly HoldBall?[] _activeBalls = new HoldBall?[HoldSystem.MaxHolds];

        // ─── イベントハンドラ（GameMainController から購読）────────

        /// <summary>slot4（index=3）に保留玉を生成する。</summary>
        public void OnHoldAdded(int index, HoldType holdType)
        {
            var ball = Instantiate(_prefab, _slots[index]);
            ball.transform.localPosition = Vector3.zero;
            ball.Setup(holdType, _sprites);
            ball.ShowAsync(destroyCancellationToken).Forget();
            _activeBalls[index] = ball;
        }

        /// <summary>保留玉を from スロットから to スロットへアニメーション移動する。</summary>
        public void OnHoldShifted(int from, int to)
        {
            var ball = _activeBalls[from];
            if (ball == null) return;

            _activeBalls[to]   = ball;
            _activeBalls[from] = null;
            ball.MoveToSlotAsync(_slots[to], destroyCancellationToken).Forget();
        }

        /// <summary>slot1（index=0）の保留玉を消化アニメーションで消す。</summary>
        public void OnHoldConsumed(int index)
        {
            var ball = _activeBalls[index];
            if (ball == null) return;

            _activeBalls[index] = null;
            ball.HideAsync(destroyCancellationToken).Forget();
        }
    }
}
#nullable disable
