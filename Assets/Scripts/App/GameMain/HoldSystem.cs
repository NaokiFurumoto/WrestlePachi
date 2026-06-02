#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// パチンコの保留システム。
    /// へそ入賞で保留を積み、最大4つまで保持する。
    /// 保留が溜まると順番に1つずつ消化してプロレス技を発動する。
    /// スロットは index 0〜3 で管理し、左から順に埋まり、左から順に消化される。
    /// </summary>
    public sealed class HoldSystem
    {
        // ─── 定数 ───────────────────────────────────────────────
        public const int MaxHolds = 4;

        /// <summary>保留1個を消化するまでの待機時間（秒）。保留アニメ相当。</summary>
        private const float ActivationDelaySec = 1.2f;

        // ─── 状態 ───────────────────────────────────────────────
        /// <summary>null = 空スロット</summary>
        private readonly HoldType?[] _slots = new HoldType?[MaxHolds];
        private bool _isActivating;

        private readonly CancellationToken _ct;

        // ─── プロパティ ──────────────────────────────────────────
        public int HoldCount
        {
            get
            {
                var count = 0;
                foreach (var s in _slots) if (s != null) count++;
                return count;
            }
        }

        // ─── イベント ────────────────────────────────────────────
        /// <summary>保留数が変化したとき発火（UI 更新用）</summary>
        public event Action<int>? OnHoldCountChanged;

        /// <summary>保留が追加されたとき発火（index: スロット番号=3固定、type: 保留種別）</summary>
        public event Action<int, HoldType>? OnHoldAdded;

        /// <summary>保留がスロット間を移動したとき発火（from→to）</summary>
        public event Action<int, int>? OnHoldShifted;

        /// <summary>保留が消化されたとき発火（index: スロット番号=0固定）</summary>
        public event Action<int>? OnHoldConsumed;

        /// <summary>技が発動するとき発火（type: 発動した保留の種別）</summary>
        public event Action<HoldType>? OnTechActivated;

        // ─── 初期化 ──────────────────────────────────────────────
        public HoldSystem(CancellationToken ct) => _ct = ct;

        // ─── 公開メソッド ────────────────────────────────────────

        /// <summary>
        /// へそ入賞時に呼ぶ。slot4（index=3）に出現させ、左へ自動シフトする。
        /// slot1（index=0）に到達したら消化シーケンスを開始する。
        /// </summary>
        public void AddHold(HoldType holdType)
        {
            // slot4 が埋まっていれば満杯（最大4保留）
            if (_slots[MaxHolds - 1] != null)
            {
                Debug.Log("[HoldSystem] 保留満杯のため無視");
                return;
            }

            // slot4 に出現
            _slots[MaxHolds - 1] = holdType;
            OnHoldAdded?.Invoke(MaxHolds - 1, holdType);
            OnHoldCountChanged?.Invoke(HoldCount);

            // 左方向へスライド（空きスロットがある限り移動）
            ShiftNewHoldLeft(MaxHolds - 1);

            // slot1 に保留が到達していて未消化なら消化開始
            if (!_isActivating && _slots[0] != null)
                ActivateSequenceAsync().Forget();
        }

        // ─── 内部処理 ────────────────────────────────────────────

        /// <summary>
        /// 追加直後の保留を左方向へスライドさせる。
        /// 左隣が空いている限り移動し、塞がったら停止。
        /// </summary>
        private void ShiftNewHoldLeft(int fromIndex)
        {
            for (var i = fromIndex; i > 0; i--)
            {
                if (_slots[i] == null || _slots[i - 1] != null) break;
                _slots[i - 1] = _slots[i];
                _slots[i]     = null;
                OnHoldShifted?.Invoke(i, i - 1);
            }
        }

        /// <summary>
        /// slot1（index=0）消化後、残り保留を全て左へ詰める。
        /// </summary>
        private void ShiftAllLeft()
        {
            for (var i = 0; i < MaxHolds - 1; i++)
            {
                if (_slots[i] != null || _slots[i + 1] == null) continue;
                _slots[i]     = _slots[i + 1];
                _slots[i + 1] = null;
                OnHoldShifted?.Invoke(i + 1, i);
            }
            OnHoldCountChanged?.Invoke(HoldCount);
        }

        /// <summary>
        /// slot1 の保留を順番に消化して技を発動するシーケンス。
        /// 保留がなくなるまで繰り返す。
        /// </summary>
        private async UniTaskVoid ActivateSequenceAsync()
        {
            _isActivating = true;

            while (!_ct.IsCancellationRequested)
            {
                if (_slots[0] == null) break;

                // 保留アニメーション待機（スロット回転相当）
                await UniTask.Delay(
                    (int)(ActivationDelaySec * 1000),
                    cancellationToken: _ct);

                if (_ct.IsCancellationRequested) break;

                var holdType = _slots[0]!.Value;
                _slots[0] = null;

                OnHoldConsumed?.Invoke(0);
                OnTechActivated?.Invoke(holdType);
                Debug.Log($"[HoldSystem] 技発動！ type={holdType} 残り保留: {HoldCount}");

                // 残り保留を左に詰める（slot2→slot1 など）
                ShiftAllLeft();
            }

            _isActivating = false;
        }

#if UNITY_EDITOR
        /// <summary>デバッグ用：現在のスロット状態のスナップショットを返す。</summary>
        public HoldType?[] Debug_GetSlots() => (HoldType?[])_slots.Clone();
#endif
    }
}
#nullable disable
