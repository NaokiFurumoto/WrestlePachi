#if UNITY_EDITOR
using System.Threading;
using App.Puyo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// デバッグ用の自動プレイエージェント。
    /// ぷよの高さを評価して最も低い列に落とす「平坦化」戦略で動作する。
    /// </summary>
    public sealed class AutoPlayAgent
    {
        // ─── 設定 ────────────────────────────────────────────────
        /// <summary>操作前の思考遅延（ms）。小さいほど高速になる。</summary>
        public int ThinkDelayMs { get; set; } = 300;

        /// <summary>移動1回あたりの遅延（ms）。</summary>
        public int MoveDelayMs  { get; set; } = 100;

        // ─── 依存 ────────────────────────────────────────────────
        private readonly IAutoPlayTarget _target;
        private readonly PuyoBoard       _board;

        private CancellationTokenSource _cts;

        // ─── 状態 ────────────────────────────────────────────────
        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        // ─── 初期化 ──────────────────────────────────────────────

        public AutoPlayAgent(IAutoPlayTarget target, PuyoBoard board)
        {
            _target = target;
            _board  = board;
        }

        // ─── 公開メソッド ────────────────────────────────────────

        public void Start()
        {
            Stop();
            _cts = new CancellationTokenSource();
            LoopAsync(_cts.Token).Forget();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        // ─── メインループ ─────────────────────────────────────────

        private async UniTaskVoid LoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var pair = _board.ActivePair;
                if (pair != null && _target.AcceptsInput)
                    await PlacePairAsync(pair, ct);
                else
                    await UniTask.Yield(cancellationToken: ct);
            }
        }

        /// <summary>
        /// 1個のペアを目標位置に誘導してソフトドロップする。
        /// ペアが他の要因でロックされた場合は途中で抜ける。
        /// </summary>
        private async UniTask PlacePairAsync(PuyoPair targetPair, CancellationToken ct)
        {
            // ─── 目標を決定 ──────────────────────────────────────
            var heights   = _board.Debug_GetColumnHeights();
            var targetCol = DecideColumn(heights);
            var targetRot = DecideRotation();

            // ─── 思考遅延 ────────────────────────────────────────
            await UniTask.Delay(ThinkDelayMs, cancellationToken: ct);
            if (ct.IsCancellationRequested || _board.ActivePair != targetPair) return;

            // ─── 回転 ────────────────────────────────────────────
            for (var i = 0; i < targetRot; i++)
            {
                if (_board.ActivePair != targetPair) return;
                _target.OnInputRotateCW();
                await UniTask.Delay(MoveDelayMs, cancellationToken: ct);
            }

            // ─── 横移動 ──────────────────────────────────────────
            while (!ct.IsCancellationRequested && _board.ActivePair == targetPair)
            {
                var col = targetPair.Debug_MainCol;
                if (col == targetCol) break;

                if (col < targetCol) _target.OnInputMoveRight();
                else                 _target.OnInputMoveLeft();

                await UniTask.Delay(MoveDelayMs, cancellationToken: ct);
            }

            if (ct.IsCancellationRequested || _board.ActivePair != targetPair) return;

            // ─── ソフトドロップ（ペアがロックされるまで）────────────
            _target.OnInputSoftDrop();
            while (!ct.IsCancellationRequested && _board.ActivePair == targetPair)
                await UniTask.Yield(cancellationToken: ct);
            _target.OnInputSoftDropEnd();
        }

        // ─── 戦略 ────────────────────────────────────────────────

        /// <summary>
        /// 列を選ぶ。高さが最も低い列を基本とし、70%はそこへ、30%はランダム列へ。
        /// 単調にならないよう揺らぎを加える。
        /// </summary>
        private int DecideColumn(int[] heights)
        {
            if (Random.value < 0.3f)
                return Random.Range(0, PuyoBoard.COLS);

            var minH    = int.MaxValue;
            var bestCol = PuyoBoard.COLS / 2;
            for (var i = 0; i < heights.Length; i++)
            {
                if (heights[i] < minH)
                {
                    minH    = heights[i];
                    bestCol = i;
                }
            }
            return bestCol;
        }

        /// <summary>
        /// 回転数（CW 回数）を決める。0〜3 の中からランダム。
        /// 縦置き（0, 2）に寄せて、チェーン素地を作りやすくする。
        /// </summary>
        private static int DecideRotation()
        {
            // 縦置き(0 or 2) : 横置き(1 or 3) = 6:4
            if (Random.value < 0.6f)
                return Random.value < 0.5f ? 0 : 2;
            return Random.value < 0.5f ? 1 : 3;
        }
    }
}
#endif
