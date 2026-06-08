#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// レインボー保留消化時に表示する PUSH! 全画面 View。
    /// OpenAsync 後に WaitForTriggerAsync を呼ぶと、タイムアウト or PUSH ボタン押下まで待機する。
    /// Prefab: Resources/Prefabs/Views/SkillRainbowView
    /// </summary>
    public sealed class SkillRainbowView : ViewBase
    {
        [Header("── 参照 ──")]
        [SerializeField, Tooltip("タイムゲージ（Timer_Gage オブジェクト）")]
        private TimerGage? _timerGage;

        [SerializeField, Tooltip("PUSH ボタン")]
        private Button? _pushButton;

        [Header("── 設定 ──")]
        [SerializeField, Tooltip("カウントダウン秒数")]
        private float _duration = 5f;

        // ─────────────────────────────────────────
        // 公開 API
        // ─────────────────────────────────────────

        /// <summary>
        /// タイムアウト または PUSH ボタン押下（tcs 完了）まで待機する。
        /// タイムアウト時はタイマー側から tcs.TrySetResult() を呼ぶ。
        /// ボタン押下・天撃キー入力どちらも tcs 経由で統一される。
        /// 外部キャンセル（ゲーム中断など）は OperationCanceledException として伝播する。
        /// </summary>
        public async UniTask WaitForTriggerAsync(UniTaskCompletionSource tcs, CancellationToken ct = default)
        {
            // タイマーキャンセル用 CTS（PUSH 後にタイマーを止める）
            using var timerCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // タイムアウト時に tcs を完了させる（fire-and-forget）
            async UniTaskVoid RunTimerAsync()
            {
                await (_timerGage?.RunAsync(_duration, timerCts.Token) ?? UniTask.CompletedTask)
                    .SuppressCancellationThrow();
                tcs.TrySetResult();
            }

            // ボタン押下 → tcs 完了
            void OnClick() => tcs.TrySetResult();
            _pushButton?.onClick.AddListener(OnClick);

            RunTimerAsync().Forget();

            try
            {
                // tcs 完了 or 外部キャンセルまで待機
                bool wasCancelled = await tcs.Task.AttachExternalCancellation(ct).SuppressCancellationThrow();

                // 外部キャンセル（ゲーム中断など）は再スロー
                if (wasCancelled && ct.IsCancellationRequested)
                    ct.ThrowIfCancellationRequested();
            }
            finally
            {
                timerCts.Cancel(); // タイマーを停止
                _pushButton?.onClick.RemoveListener(OnClick);
            }
        }
    }
}
