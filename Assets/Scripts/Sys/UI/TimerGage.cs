#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GameSys
{
    /// <summary>
    /// パチンコ演出向けのタイムゲージコンポーネント。
    /// Image（Type: Filled / Fill Method: Horizontal）の fillAmount を 1→0 へ減らす。
    /// RunAsync が完了 = タイムアウト。外部キャンセルでも正しく停止する。
    /// </summary>
    public sealed class TimerGage : MonoBehaviour
    {
        [SerializeField, Tooltip("fillAmount を操作する Image（Type: Filled にすること）")]
        private Image? _fillImage;

        private Tween? _fillTween;

        /// <summary>現在の残り時間割合（1=満タン, 0=タイムアウト）。</summary>
        public float NormalizedRemaining => _fillImage != null ? _fillImage.fillAmount : 0f;

        /// <summary>
        /// カウントダウンを開始する。
        /// duration 秒経過でタイムアウト完了。ct がキャンセルされた場合は OperationCanceledException を投げる。
        /// </summary>
        public async UniTask RunAsync(float duration, CancellationToken ct = default)
        {
            if (_fillImage == null)
            {
                Debug.LogWarning("[TimerGage] _fillImage が未設定です。");
                return;
            }

            // 開始前にゲージをリセット
            ResetGage();

            var tcs = new UniTaskCompletionSource();

            _fillTween = _fillImage
                .DOFillAmount(0f, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() => tcs.TrySetResult())
                .OnKill(() => tcs.TrySetResult())
                .Play();

            try
            {
                await tcs.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                // キャンセル時もTweenを確実に止める
                _fillTween?.Kill(complete: false);
                _fillTween = null;
            }
        }

        /// <summary>カウントダウンを即時停止する。</summary>
        public void Stop()
        {
            _fillTween?.Kill(complete: false);
            _fillTween = null;
        }

        /// <summary>ゲージを満タンに戻す。</summary>
        public void ResetGage()
        {
            Stop();
            if (_fillImage != null) _fillImage.fillAmount = 1f;
        }

        private void OnDestroy() => Stop();
    }
}
