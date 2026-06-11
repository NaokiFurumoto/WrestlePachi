#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Simulator
{
    /// <summary>
    /// パチンコ盤面シミュレーター。
    /// 指定数の玉を自動発射して入賞率を計測する。
    /// ぷよの落下・保留の発生はすべて無効化した状態で動作する。
    /// </summary>
    public sealed class PachinkoSimulator : MonoBehaviour
    {
        // ─── Inspector 参照 ──────────────────────────────────────
        [SerializeField] private GameMainController _gameMainController = null!;
        [SerializeField] private BallLauncher       _ballLauncher       = null!;
        [SerializeField] private HesoTrigger        _hesoTrigger        = null!;

        // ─── シミュレーション設定 ─────────────────────────────────
        [Header("シミュレーション設定")]
        [SerializeField, Min(1)]          private int   _defaultBallCount  = 250;
        [SerializeField, Range(1f, 8f)]   private float _defaultTimeScale  = 2f;
        [SerializeField, Min(0.05f)]      private float _launchIntervalSec = 0.4f; // 発射間隔（実時間）

        // ─── 公開プロパティ ───────────────────────────────────────
        public bool  IsRunning    { get; private set; }
        public int   LaunchCount  { get; private set; }
        public int   EntryCount   { get; private set; }
        public int   TargetCount  { get; private set; }
        public float EntryRate    => LaunchCount > 0 ? (float)EntryCount / LaunchCount : 0f;

        /// <summary>発射数・入賞数が変化するたびに発火する</summary>
        public event Action? OnStatsUpdated;

        /// <summary>シミュレーション完了時に発火する</summary>
        public event Action? OnCompleted;

        // ─── 内部状態 ────────────────────────────────────────────
        private CancellationTokenSource? _cts;

        // ─── 公開 API ────────────────────────────────────────────

        /// <summary>シミュレーションを開始する。</summary>
        public void StartSimulation(int ballCount, float timeScale)
        {
            if (IsRunning)
            {
                Debug.LogWarning("[PachinkoSimulator] すでに実行中です。先に StopSimulation を呼んでください。");
                return;
            }

            IsRunning    = true;
            LaunchCount  = 0;
            EntryCount   = 0;
            TargetCount  = ballCount;

            // へそ入賞を直接カウント
            _hesoTrigger.OnEntered += OnHesoEntered;

            // ぷよ停止・保留無効化
            _gameMainController.SetSimulatorMode(true);

            // 高速化
            Time.timeScale = timeScale;

            // 発射ループ開始
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            RunAsync(ballCount, _cts.Token).Forget();
        }

        /// <summary>デフォルト設定でシミュレーションを開始する（View から呼ぶ用）。</summary>
        public void StartSimulation() => StartSimulation(_defaultBallCount, _defaultTimeScale);

        /// <summary>シミュレーションを途中で停止する。</summary>
        public void StopSimulation()
        {
            if (!IsRunning) return;
            _cts?.Cancel();
            // Cleanup は RunAsync のキャンセル後に呼ばれる
        }

        // ─── 内部処理 ────────────────────────────────────────────

        private async UniTaskVoid RunAsync(int ballCount, CancellationToken ct)
        {
            // timeScale が変わるので発射間隔は実時間ベースで指定
            var intervalMs = Mathf.RoundToInt(_launchIntervalSec / Time.timeScale * 1000f);

            for (var i = 0; i < ballCount; i++)
            {
                if (ct.IsCancellationRequested) break;

                _ballLauncher.LaunchOneForSimulator();
                LaunchCount++;
                OnStatsUpdated?.Invoke();

                await UniTask.Delay(intervalMs, cancellationToken: ct).SuppressCancellationThrow();
                if (ct.IsCancellationRequested) break;
            }

            Cleanup(completed: !ct.IsCancellationRequested);
        }

        private void OnHesoEntered()
        {
            EntryCount++;
            OnStatsUpdated?.Invoke();
        }

        private void Cleanup(bool completed)
        {
            IsRunning = false;

            _hesoTrigger.OnEntered           -= OnHesoEntered;
            _gameMainController.SetSimulatorMode(false);

            Time.timeScale = 1f;
            _ballLauncher.ClearAllBalls();

            OnStatsUpdated?.Invoke();

            if (completed) OnCompleted?.Invoke();
        }
    }
}
#nullable disable
