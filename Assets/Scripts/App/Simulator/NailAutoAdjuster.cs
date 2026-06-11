#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Simulator
{
    /// <summary>
    /// パチンコ釘の自動調整コンポーネント。
    /// ヒルクライミング法でシミュレーションを繰り返し、
    /// 入賞率が目標値（8%）に近づくように釘位置を調整する。
    ///
    /// Inspector で調整対象釘リストと移動範囲を設定してから
    /// StartAdjustment() を呼ぶ。
    /// 採用した変更は PendingAdjustments に記録され、
    /// EditorWindow がPlayモード終了後に Undo 対応で適用する。
    /// </summary>
    public sealed class NailAutoAdjuster : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("調整対象")]
        [SerializeField] private Transform[] _adjustableNails = Array.Empty<Transform>();

        [Header("調整パラメータ")]
        [SerializeField, Min(0.01f)]      private float _moveRange       = 0.15f;
        [SerializeField, Range(1, 50)]    private int   _maxTrials       = 10;
        [SerializeField, Min(0.01f)]      private float _stepSize        = 0.05f;

        [Header("シミュレーション設定")]
        [SerializeField, Min(1)]          private int   _simBallCount    = 125;
        [SerializeField, Range(1f, 8f)]   private float _simTimeScale    = 2f;

        [Header("目標")]
        [SerializeField, Range(0f, 1f)]   private float _targetRate      = 0.08f;
        [SerializeField, Range(0f, 0.1f)] private float _passMargin      = 0.015f;

        [Header("参照")]
        [SerializeField] private PachinkoSimulator _simulator = null!;

        // ─── 公開プロパティ ───────────────────────────────────────
        public bool   IsAdjusting  { get; private set; }
        public int    CurrentTrial { get; private set; }
        public float  LastRate     { get; private set; }
        public string StatusText   { get; private set; } = "";
        public int    MaxTrials    => _maxTrials;

        public event Action? OnTrialCompleted;
        public event Action? OnAdjustmentCompleted;

        // ─── 変更記録（static: PlayMode終了後もEditorに残る） ────────
        /// <summary>採用した変更の記録。(hierarchyPath, worldPos) のリスト。</summary>
        public static readonly List<(string HierarchyPath, Vector3 WorldPos)> PendingAdjustments = new();

        // ─── 内部状態 ────────────────────────────────────────────
        private CancellationTokenSource?        _cts;
        private readonly Dictionary<Transform, Vector3> _startPositions = new();
        private readonly System.Random          _rng = new();
        private Transform[]                     _validNails = Array.Empty<Transform>();

        // ─── 公開 API ────────────────────────────────────────────

        public void StartAdjustment()
        {
            if (IsAdjusting) return;

            if (_simulator == null)
            {
                Debug.LogWarning("[NailAutoAdjuster] Inspector の Simulator に PachinkoSimulator をアサインしてください。");
                return;
            }

            // nullを除いた有効な釘だけ使う
            _validNails = System.Array.FindAll(_adjustableNails, n => n != null);
            if (_validNails.Length == 0)
            {
                Debug.LogWarning("[NailAutoAdjuster] Inspector の _adjustableNails に釘をアサインしてください。");
                return;
            }

            IsAdjusting = true;
            CurrentTrial = 0;
            PendingAdjustments.Clear();
            _startPositions.Clear();

            // 開始時の位置を記録（移動範囲の基準にする）
            foreach (var nail in _validNails)
                _startPositions[nail] = nail.position;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            AdjustAsync(_cts.Token).Forget();
        }

        public void StopAdjustment()
        {
            if (!IsAdjusting) return;
            _cts?.Cancel();
        }

        // ─── 調整ループ ───────────────────────────────────────────

        private async UniTaskVoid AdjustAsync(CancellationToken ct)
        {
            // 初回測定
            StatusText = "初回測定中...";
            LastRate = await RunOneSimulationAsync(ct);
            if (ct.IsCancellationRequested) { Finish(canceled: true); return; }

            for (int trial = 1; trial <= _maxTrials; trial++)
            {
                CurrentTrial = trial;

                // 目標範囲内なら終了
                if (Mathf.Abs(LastRate - _targetRate) <= _passMargin)
                {
                    StatusText = $"★ 目標達成！ ({LastRate * 100f:F1}%)";
                    break;
                }

                // ランダムに釘を選んで動かす
                var nail = _validNails[_rng.Next(_validNails.Length)];
                var prevPos = nail.position;

                var candidate = NextCandidatePosition(nail, prevPos);
                nail.position = candidate;

                StatusText = $"試行 {trial}/{_maxTrials}: シミュレーション中...";
                var newRate = await RunOneSimulationAsync(ct);

                if (ct.IsCancellationRequested)
                {
                    nail.position = prevPos; // キャンセル時は元に戻す
                    Finish(canceled: true);
                    return;
                }

                // 目標との距離が縮まったか判定
                if (Mathf.Abs(newRate - _targetRate) < Mathf.Abs(LastRate - _targetRate))
                {
                    LastRate = newRate;
                    RecordChange(nail, candidate);
                    StatusText = $"試行 {trial}/{_maxTrials}: 採用 → {newRate * 100f:F1}%";
                }
                else
                {
                    nail.position = prevPos;
                    StatusText = $"試行 {trial}/{_maxTrials}: 棄却 ({newRate * 100f:F1}%)";
                }

                OnTrialCompleted?.Invoke();
                await UniTask.Yield(ct).SuppressCancellationThrow();
                if (ct.IsCancellationRequested) { Finish(canceled: true); return; }
            }

            Finish(canceled: false);
        }

        // ─── 1回のシミュレーション実行・完了待ち ─────────────────────

        private async UniTask<float> RunOneSimulationAsync(CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource<float>();

            void OnCompleted()
            {
                _simulator.OnCompleted -= OnCompleted;
                tcs.TrySetResult(_simulator.EntryRate);
            }

            _simulator.OnCompleted += OnCompleted;
            _simulator.StartSimulation(_simBallCount, _simTimeScale);

            var (canceled, rate) = await tcs.Task
                .AttachExternalCancellation(ct)
                .SuppressCancellationThrow();

            if (canceled)
                _simulator.StopSimulation();

            return canceled ? _simulator.EntryRate : rate;
        }

        // ─── ヘルパー ────────────────────────────────────────────

        /// <summary>釘を x/y いずれかの軸でランダムに ±step 動かした候補座標を返す。</summary>
        private Vector3 NextCandidatePosition(Transform nail, Vector3 currentPos)
        {
            var useX   = _rng.Next(2) == 0;
            var sign   = _rng.Next(2) == 0 ? 1f : -1f;
            var delta  = (useX ? Vector3.right : Vector3.up) * (sign * _stepSize);
            var origin = _startPositions.TryGetValue(nail, out var s) ? s : currentPos;
            return ClampToRange(currentPos + delta, origin);
        }

        private Vector3 ClampToRange(Vector3 candidate, Vector3 origin) => new(
            Mathf.Clamp(candidate.x, origin.x - _moveRange, origin.x + _moveRange),
            Mathf.Clamp(candidate.y, origin.y - _moveRange, origin.y + _moveRange),
            candidate.z);

        private void RecordChange(Transform nail, Vector3 worldPos)
        {
            var path = GetHierarchyPath(nail);
            for (int i = 0; i < PendingAdjustments.Count; i++)
            {
                if (PendingAdjustments[i].HierarchyPath == path)
                {
                    PendingAdjustments[i] = (path, worldPos);
                    return;
                }
            }
            PendingAdjustments.Add((path, worldPos));
        }

        private static string GetHierarchyPath(Transform t)
        {
            var path = t.name;
            var p    = t.parent;
            while (p != null) { path = p.name + "/" + path; p = p.parent; }
            return path;
        }

        private void Finish(bool canceled)
        {
            IsAdjusting = false;
            if (canceled && string.IsNullOrEmpty(StatusText))
                StatusText = "キャンセルされました";
            OnAdjustmentCompleted?.Invoke();
        }
    }
}
#nullable disable
