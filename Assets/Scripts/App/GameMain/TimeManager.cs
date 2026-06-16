#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameSys;
using UnityEngine;

namespace App
{
    /// <summary>
    /// Time.timeScale を一元管理するシングルトン。
    /// ポーズ・ヒットストップ・タイムスローをどこからでも呼べる。
    /// </summary>
    public sealed class TimeManager : BehaviourSingleton<TimeManager>
    {
        [SerializeField] private float _defaultScale = 1f;

        private Tween? _tween;
        private int    _pauseDepth;

        // ── BehaviourSingleton ────────────────────────────────────────

        protected override void _SetInstance() => s_Instance = this;

        protected override void _OnDestroy()
        {
            _tween?.Kill();
            Time.timeScale = 1f;
        }

        // ── 静的 API ─────────────────────────────────────────────────

        public static bool IsPaused => isValid && Instance._pauseDepth > 0;

        public static void Pause()  => Instance?.DoPause();
        public static void Resume() => Instance?.DoResume();

        public static void HitStop(float duration)
            => Instance?.DoHitStopAsync(duration, CancellationToken.None).Forget();

        public static UniTask HitStopAsync(float duration, CancellationToken ct)
            => isValid ? Instance.DoHitStopAsync(duration, ct) : UniTask.CompletedTask;

        public static void TimeSlow(float scale = 0.15f, float duration = 0.5f, float recover = 0.2f)
            => Instance?.DoTimeSlowAsync(scale, duration, recover, CancellationToken.None).Forget();

        public static UniTask TimeSlowAsync(float scale, float duration, float recover, CancellationToken ct)
            => isValid ? Instance.DoTimeSlowAsync(scale, duration, recover, ct) : UniTask.CompletedTask;

        // ── 内部処理 ─────────────────────────────────────────────────

        private void DoPause()
        {
            _pauseDepth++;
            if (_pauseDepth == 1) Time.timeScale = 0f;
        }

        private void DoResume()
        {
            _pauseDepth = Mathf.Max(0, _pauseDepth - 1);
            if (_pauseDepth == 0) Time.timeScale = _defaultScale;
        }

        private async UniTask DoHitStopAsync(float duration, CancellationToken ct)
        {
            _tween?.Kill();
            Time.timeScale = 0f;
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), DelayType.Realtime, cancellationToken: ct);
            }
            finally
            {
                if (!IsPaused) Time.timeScale = _defaultScale;
            }
        }

        private async UniTask DoTimeSlowAsync(float scale, float duration, float recover, CancellationToken ct)
        {
            _tween?.Kill();
            Time.timeScale = scale;
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), DelayType.Realtime, cancellationToken: ct);

                _tween = DOTween
                    .To(() => Time.timeScale, v => Time.timeScale = v, _defaultScale, recover)
                    .SetUpdate(true);
                await UniTask.Delay(TimeSpan.FromSeconds(recover), DelayType.Realtime, cancellationToken: ct);
            }
            finally
            {
                if (!IsPaused) Time.timeScale = _defaultScale;
            }
        }
    }
}
#nullable disable
