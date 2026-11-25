#nullable enable
using System;
using System.Collections.Generic;
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

        /// <summary>鎖のかかった保留が砕けて消えるまでの時間（秒）。この間は次の保留を詰めない（HoldBall の砕ける演出に合わせる）</summary>
        private const float LockedFizzleSec = 0.5f;

        /// <summary>へそ入賞後、保留玉が画面に出るまでの待機時間（秒）。実機スロット演出相当。</summary>
        private const float HoldRevealDelaySec = 1.0f;

        // ─── 状態 ───────────────────────────────────────────────
        /// <summary>null = 空スロット</summary>
        private readonly HoldType?[] _slots = new HoldType?[MaxHolds];
        /// <summary>敵スキル「スキル封印」で鎖がかかっている保留（スロットと同じ並び）</summary>
        private readonly bool[] _locked = new bool[MaxHolds];
        /// <summary>敵が狙っている：次に出てきた（ロックできる）保留をロックする</summary>
        private bool _lockRequested;
        private bool _isActivating;

        // RevealHoldAsync の遅延中にシフトが発生した場合に正しい index を伝えるための参照
        private readonly List<int[]> _pendingRevealIndices = new();

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

        /// <summary>保留に鎖がかかったとき発火（index: 表示されたスロット番号）。保留玉が画面に出た直後に呼ぶ</summary>
        public event Action<int>? OnHoldLocked;

        /// <summary>鎖のかかった保留が消化され、不発になったとき発火（技は出ない）</summary>
        public event Action<HoldType>? OnLockedHoldFizzled;

        /// <summary>
        /// 技が発動するとき呼ばれる非同期ハンドラ。
        /// 返した UniTask が完了するまでシーケンスは次の保留へ進まない。
        /// </summary>
        public Func<HoldType, UniTask>? OnTechActivated;

        // ─── 初期化 ──────────────────────────────────────────────
        public HoldSystem(CancellationToken ct) => _ct = ct;

        // ─── 公開メソッド ────────────────────────────────────────

        /// <summary>敵が狙っているか（次に出てきた保留をロックする待ち）</summary>
        public bool HasLockRequest => _lockRequested;

        /// <summary>指定スロットの保留に鎖がかかっているか</summary>
        public bool IsLocked(int index) => index >= 0 && index < MaxHolds && _slots[index] != null && _locked[index];

        /// <summary>鎖をかけられる保留か。虹（希望を残す）と黒（かけるとプレイヤーが得をする）はかけない</summary>
        public static bool IsLockable(HoldType type) => type != HoldType.Rainbow && type != HoldType.Black;

        /// <summary>敵スキル「スキル封印」：次に出てくる（ロックできる）保留を1つロックする。重ねて呼んでも1つだけ</summary>
        public void RequestLock() => _lockRequested = true;

        /// <summary>狙いを取り消す（ステージ終了・技が外れたとき）</summary>
        public void CancelLockRequest() => _lockRequested = false;

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
                return;
            }

            _slots[index]  = holdType;
            _locked[index] = false;
            if (_lockRequested && IsLockable(holdType))
            {
                _locked[index] = true;
                _lockRequested = false;
            }
            OnHoldCountChanged?.Invoke(HoldCount);

            // 表示は遅延後に通知（実機スロット演出相当）
            // indexRef: ShiftAllLeft でシフトが発生した場合に最新インデックスへ更新される
            var indexRef = new int[] { index };
            _pendingRevealIndices.Add(indexRef);
            RevealHoldAsync(indexRef, holdType).Forget();

            // slot0 に配置されたとき、未消化なら消化シーケンス開始
            if (index == 0 && !_isActivating)
                ActivateSequenceAsync().Forget();
        }

        // ─── 内部処理 ────────────────────────────────────────────

        private async UniTaskVoid RevealHoldAsync(int[] indexRef, HoldType holdType)
        {
            await UniTask.Delay((int)(HoldRevealDelaySec * 1000), cancellationToken: _ct)
                .SuppressCancellationThrow();
            _pendingRevealIndices.Remove(indexRef);
            if (_ct.IsCancellationRequested) return;
            OnHoldAdded?.Invoke(indexRef[0], holdType);
            if (IsLocked(indexRef[0])) OnHoldLocked?.Invoke(indexRef[0]);
        }

        /// <summary>
        /// slot1（index=0）消化後、残り保留を全て左へ詰める。
        /// </summary>
        private void ShiftAllLeft()
        {
            for (var i = 0; i < MaxHolds - 1; i++)
            {
                if (_slots[i] != null || _slots[i + 1] == null) continue;
                _slots[i]      = _slots[i + 1];
                _slots[i + 1]  = null;
                _locked[i]     = _locked[i + 1];
                _locked[i + 1] = false;

                // RevealHoldAsync が遅延中の場合、そのインデックスも同期して更新する
                foreach (var idxRef in _pendingRevealIndices)
                {
                    if (idxRef[0] == i + 1) idxRef[0] = i;
                }

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
            try
            {
                while (!_ct.IsCancellationRequested)
                {
                    if (_slots[0] == null) break;

                    // 保留アニメーション待機（スロット回転相当）
                    // UnscaledDeltaTime: ポーズ中は止まるが HitStop/TimeSlow の影響を受けない
                    await UniTask.Delay(
                        (int)(ActivationDelaySec * 1000),
                        DelayType.UnscaledDeltaTime,
                        cancellationToken: _ct)
                        .SuppressCancellationThrow();

                    if (_ct.IsCancellationRequested) break;

                    // 鎖のかかった保留は不発：玉が砕け終わるまで先頭に置いたまま待ち、それから左へ詰める
                    // （先頭を空けたまま待つと、その間の新しい保留が先頭に入って順番が崩れ、
                    //  すぐ詰めると次の玉が砕けている玉に重なって見える）
                    if (_locked[0])
                    {
                        var lockedType = _slots[0]!.Value;
                        OnHoldConsumed?.Invoke(0);
                        await UniTask.Delay((int)(LockedFizzleSec * 1000), DelayType.UnscaledDeltaTime, cancellationToken: _ct)
                            .SuppressCancellationThrow();
                        if (_ct.IsCancellationRequested) break;
                        _slots[0]  = null;
                        _locked[0] = false;
                        ShiftAllLeft();
                        OnLockedHoldFizzled?.Invoke(lockedType);
                        continue;
                    }

                    var next = TakeNext();

                    // スキル演出が完了するまで待機してから次の保留へ進む
                    if (OnTechActivated != null)
                        await OnTechActivated.Invoke(next.Type);
                }
            }
            finally
            {
                _isActivating = false;
            }
        }

        /// <summary>
        /// 先頭（slot0）の保留を取り出し、鎖がかかっていたかを返す。OnHoldConsumed を発火し、
        /// 残りを左へ詰める（スキル演出中に新保留が入っても最後尾に入るように、消化直後に詰める）。
        /// </summary>
        public (HoldType Type, bool Locked) TakeNext()
        {
            var holdType = _slots[0]!.Value;
            var locked   = _locked[0];
            _slots[0]  = null;
            _locked[0] = false;

            OnHoldConsumed?.Invoke(0);
            ShiftAllLeft();
            return (holdType, locked);
        }

#if UNITY_EDITOR
        /// <summary>デバッグ用：現在のスロット状態のスナップショットを返す。</summary>
        public HoldType?[] Debug_GetSlots() => (HoldType?[])_slots.Clone();
#endif
    }
}
#nullable disable
