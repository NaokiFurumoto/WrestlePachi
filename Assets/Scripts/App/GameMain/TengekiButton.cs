#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;
using UnityEngine.EventSystems;

namespace App
{
    /// <summary>
    /// 天撃ボタンの演出を管理するコンポーネント。
    /// タップ検知は IPointerDownHandler（要 BoxCollider2D + PhysicsRaycaster2D）。
    /// ストック保留の色グローは TengekiGlowController 経由で制御する。
    /// </summary>
    public sealed class TengekiButton : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private UIAnimator? _animator;

        private TengekiGlowController? _glow;
        private CancellationTokenSource? _bobCts;

        /// <summary>タップ/クリックされたとき発火する。</summary>
        public event Action? OnClicked;

        private void Awake()
        {
            _glow = GetComponentInChildren<TengekiGlowController>();
        }

        public void OnPointerDown(PointerEventData _) => OnClicked?.Invoke();

        /// <summary>
        /// ストック保留の色でグローを点灯する。null でデフォルトに戻す。
        /// </summary>
        public void SetStockGlow(HoldType? holdType)
        {
            if (_glow == null) return;

            if (!holdType.HasValue) { _glow.ShowNormal(); return; }

            switch (holdType.Value)
            {
                case HoldType.Red:    _glow.SetRed();    break;
                case HoldType.Yellow: _glow.SetYellow(); break;
                case HoldType.Green:  _glow.SetGreen();  break;
                case HoldType.Blue:   _glow.SetBlue();   break;
                case HoldType.Purple: _glow.SetCustomColor(new Color(0.70f, 0.20f, 1.0f)); break;
                default:              _glow.ShowNormal(); break;
            }
        }

        /// <summary>
        /// ストック発動可能時に Bob ループを開始/停止する。
        /// _animator.BobAsync() を直接呼ぶため、Btn_Main の AnimType 設定に依存しない。
        /// </summary>
        public void SetBobActive(bool active)
        {
            if (_animator == null) return;

            if (!active)
            {
                _bobCts?.Cancel();
                _bobCts?.Dispose();
                _bobCts = null;
                return;
            }

            if (_bobCts != null && !_bobCts.IsCancellationRequested) return;

            _bobCts?.Dispose();
            _bobCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            BobLoopAsync(_bobCts.Token).Forget();
        }

        private async UniTaskVoid BobLoopAsync(CancellationToken ct)
        {
            if (_animator == null) return;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    bool cancelled = await _animator.BobAsync(ct).SuppressCancellationThrow();
                    if (cancelled) break;
                }
            }
            finally
            {
                _animator.Stop();
            }
        }

        /// <summary>「今は使えない」を伝える1回シェイク。</summary>
        public void ShakeOnce()
        {
            if (_animator == null) return;
            _animator.PlayAsync(destroyCancellationToken).Forget();
        }

        /// <summary>
        /// 虹保留が入っている間シェイクをループする。
        /// ct がキャンセルされると自動停止してボタンを元の状態に戻す。
        /// </summary>
        public async UniTaskVoid StartShakeAsync(CancellationToken ct)
        {
            if (_animator == null) return;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    bool cancelled = await _animator.PlayAsync(ct).SuppressCancellationThrow();
                    if (cancelled) break;
                }
            }
            finally
            {
                _animator.Stop();
            }
        }
    }
}
#nullable disable
