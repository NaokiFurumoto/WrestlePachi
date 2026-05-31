#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace App
{
    // ─── Strategy インターフェース ────────────────────────────────────────

    /// <summary>ボタン入力の振る舞いを抽象化するStrategy</summary>
    public interface IInputButtonStrategy
    {
        void OnPointerDown();
        void OnPointerUp();
    }

    /// <summary>押下時に1回だけ発火。指を離すまで再発火しない（回転ボタン用）</summary>
    public sealed class SingleFireStrategy : IInputButtonStrategy
    {
        private readonly Action _action;
        private bool _fired;

        public SingleFireStrategy(Action action) => _action = action;

        public void OnPointerDown() { if (!_fired) { _fired = true; _action(); } }
        public void OnPointerUp()   => _fired = false;
    }

    /// <summary>
    /// 押下直後に即発火し、_initialDelay 後からリピート発火（左右移動用）。
    /// UniTask で非同期リピートループを管理する。
    /// </summary>
    public sealed class RepeatFireStrategy : IInputButtonStrategy
    {
        private readonly Action            _action;
        private readonly CancellationToken _appToken;
        private readonly float             _initialDelay;
        private readonly float             _repeatInterval;
        private CancellationTokenSource?   _cts;

        public RepeatFireStrategy(
            Action action,
            CancellationToken appToken,
            float initialDelay   = 0.25f,
            float repeatInterval = 0.1f)
        {
            _action         = action;
            _appToken       = appToken;
            _initialDelay   = initialDelay;
            _repeatInterval = repeatInterval;
        }

        public void OnPointerDown()
        {
            _action();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(_appToken);
            RepeatLoopAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid RepeatLoopAsync(CancellationToken token)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_initialDelay), cancellationToken: token);
            while (!token.IsCancellationRequested)
            {
                _action();
                await UniTask.Delay(TimeSpan.FromSeconds(_repeatInterval), cancellationToken: token);
            }
        }

        public void OnPointerUp()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }

    /// <summary>押している間だけ OnDown を維持し、離したら OnUp を呼ぶ（ソフトドロップ用）</summary>
    public sealed class HoldStrategy : IInputButtonStrategy
    {
        private readonly Action _onDown;
        private readonly Action _onUp;

        public HoldStrategy(Action onDown, Action onUp) { _onDown = onDown; _onUp = onUp; }

        public void OnPointerDown() => _onDown();
        public void OnPointerUp()   => _onUp();
    }

    // ─── InputButton ──────────────────────────────────────────────────────

    /// <summary>
    /// Button に追加し、PointerDown / PointerUp を Strategy に委譲する。
    /// 押下時に拡縮アニメーションを再生する。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class InputButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private IInputButtonStrategy? _strategy;
        private Button?               _button;

        private void Awake() => _button = GetComponent<Button>();

        public void Setup(IInputButtonStrategy strategy) => _strategy = strategy;

        public void OnPointerDown(PointerEventData _)
        {
            if (_button != null && !_button.interactable) return;
            transform.DOKill();
            transform.DOScale(0.88f, 0.07f).SetEase(Ease.OutQuad);
            _strategy?.OnPointerDown();
        }

        public void OnPointerUp(PointerEventData _)
        {
            transform.DOKill();
            transform.DOScale(1f, 0.1f).SetEase(Ease.OutBack);
            _strategy?.OnPointerUp();
        }
    }
}
#nullable disable
