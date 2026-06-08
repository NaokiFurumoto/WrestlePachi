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

        /// <summary>slot0 到達後、消化までの待機時間（秒）。スロット回転演出相当。</summary>
        private const float ActivationDelaySec = 2.0f;

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

        /// <summary>保留が追加されたとき発火（index: 配置されたスロット番号、type: 保留種別）</summary>
        public event Action<int, HoldType>? OnHoldAdded;

        /// <summary>保留がスロット間を移動したとき発火（from→to）</summary>
        public event Action<int, int>? OnHoldShifted;

        /// <summary>保留が消化されたとき発火（index: スロット番号=0固定）</summary>
        public event Action<int>? OnHoldConsumed;

        /// <summary>
        /// 技が発動するとき呼ばれる非同期ハンドラ。
        /// 返した UniTask が完了するまでシーケンスは次の保留へ進まない。
        /// </summary>
        public Func<HoldType, UniTask>? OnTechActivated;

        // ─── 初期化 ──────────────────────────────────────────────
        public HoldSystem(CancellationToken ct) => _ct = ct;

        // ─── 公開メソッド ────────────────────────────────────────

        /// <summary>
        /// へそ入賞時に呼ぶ。左から最初の空きスロットに直接配置する。
        /// slot0 に配置された場合は消化シーケンスを開始する。
        /// </summary>
        public void AddHold(HoldType holdType)
        {
            // 左から最初の空きスロットを探す
            var index = -1;
            for (var i = 0; i < MaxHolds; i++)
            {
                if (_slots[i] != null) continue;
                index = i;
                break;
            }

            if (index < 0)
            {
                Debug.Log("[HoldSystem] 保留満杯のため無視");
                return;
            }

            _slots[index] = holdType;
            OnHoldAdded?.Invoke(index, holdType);
            OnHoldCountChanged?.Invoke(HoldCount);

            // slot0 に配置されたとき、未消化なら消化シーケンス開始
            if (index == 0 && !_isActivating)
                ActivateSequenceAsync().Forget();
        }

        // ─── 内部処理 ────────────────────────────────────────────

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

                // 消化直後に左詰めする（スキル演出中に新保留が入っても最後尾に入るように）
                ShiftAllLeft();
                Debug.Log($"[HoldSystem] 技発動！ type={holdType}  残り保留: {HoldCount}");

                // スキル演出が完了するまで待機してから次の保留へ進む
                if (OnTechActivated != null)
                    await OnTechActivated.Invoke(holdType);
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
