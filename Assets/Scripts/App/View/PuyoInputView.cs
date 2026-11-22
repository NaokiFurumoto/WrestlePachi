#nullable enable
using DG.Tweening;
using GameSys;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// タッチ操作UIのView。
    /// 5ボタン（左・右・CW・CCW・ソフトドロップ）で PuyoPair を操作する。
    /// Prefab: Resources/Prefabs/Views/PiyoInputView
    /// </summary>
    public sealed class PuyoInputView : ViewBase
    {

        /// <summary>コントローラー参照を渡す初期化データ</summary>
        public class InputViewData : ViewData
        {
            public GameMainController? Controller { get; set; }
        }

        [UnityEngine.SerializeField] private Button? _btnLeft;
        [UnityEngine.SerializeField] private Button? _btnRight;
        [UnityEngine.SerializeField] private Button? _btnRotateCW;
        [UnityEngine.SerializeField] private Button? _btnRotateCCW;
        [UnityEngine.SerializeField] private Button? _btnSoftDrop;

        [UnityEngine.Header("敵スキル「混乱」中の左右ボタン")]
        [UnityEngine.Tooltip("紫の左矢印（混乱中は右ボタンに出す）")]
        [UnityEngine.SerializeField] private Sprite? _confusedLeftArrow;
        [UnityEngine.Tooltip("紫の右矢印（混乱中は左ボタンに出す）")]
        [UnityEngine.SerializeField] private Sprite? _confusedRightArrow;

        /// <summary>ボタンを裏返す時間（半分で絵を差し替える）と、混乱中にふらふら揺れる角度</summary>
        private const float FlipTime = 0.24f;
        private const float WobbleDegrees = 7f;

        private GameMainController? _controller;
        private Sprite? _leftArrow;
        private Sprite? _rightArrow;
        private bool _showingReversed;

        // ─── ViewBase override ───────────────────────────────────────

        protected override void OnInitialize()
        {
            if (Data is InputViewData data)
                _controller = data.Controller;

            _leftArrow  = _btnLeft  != null ? _btnLeft.image.sprite  : null;
            _rightArrow = _btnRight != null ? _btnRight.image.sprite : null;
            BindButtons();
        }

        // ─── 混乱の表示 ──────────────────────────────────────────────

        /// <summary>
        /// ボタンに出す矢印の絵。混乱中は押すと逆に動くので、左ボタンに紫の右矢印・右ボタンに紫の左矢印を出す
        /// （紫の絵がなければ普通の矢印を入れ替える）。
        /// </summary>
        public static Sprite? ArrowSpriteFor(bool isLeftButton, bool reversed,
            Sprite? left, Sprite? right, Sprite? confusedLeft, Sprite? confusedRight)
        {
            if (!reversed) return isLeftButton ? left : right;
            return isLeftButton ? (confusedRight != null ? confusedRight : right)
                                : (confusedLeft  != null ? confusedLeft  : left);
        }

        private void Update()
        {
            var reversed = _controller != null && _controller.IsInputReversed;
            if (reversed == _showingReversed) return;
            _showingReversed = reversed;
            FlipButton(_btnLeft,  isLeftButton: true,  reversed);
            FlipButton(_btnRight, isLeftButton: false, reversed);
        }

        /// <summary>ボタンをくるっと裏返して矢印を差し替える。混乱中はふらふら揺らし続ける</summary>
        private void FlipButton(Button? btn, bool isLeftButton, bool reversed)
        {
            if (btn == null) return;
            var t = btn.transform;
            t.DOKill(complete: false);
            var sprite = ArrowSpriteFor(isLeftButton, reversed, _leftArrow, _rightArrow, _confusedLeftArrow, _confusedRightArrow);
            // 左右のボタンを逆向きに揺らして、目が回っている感じにする
            var dir = isLeftButton ? 1f : -1f;

            DOTween.Sequence()
                .Append(t.DOScaleX(0f, FlipTime * 0.5f).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    // 細くなって見えない瞬間に、絵と傾きを差し替える
                    btn.image.sprite = sprite;
                    t.localRotation = Quaternion.Euler(0f, 0f, reversed ? -WobbleDegrees * dir : 0f);
                })
                .Append(t.DOScaleX(1f, FlipTime * 0.5f).SetEase(Ease.OutBack))
                .SetTarget(t)
                .SetUpdate(true)
                .SetLink(btn.gameObject);

            if (!reversed) return;
            t.DOLocalRotate(new Vector3(0f, 0f, WobbleDegrees * dir), 0.7f)
             .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
             .SetDelay(FlipTime).SetUpdate(true).SetLink(btn.gameObject);
        }

        // ─── ボタンバインド ──────────────────────────────────────────

        private void BindButtons()
        {
            var token = destroyCancellationToken;

            // 左右移動：長押しリピート
            Bind(_btnLeft,      new RepeatFireStrategy(() => _controller?.OnInputMoveLeft(),  token));
            Bind(_btnRight,     new RepeatFireStrategy(() => _controller?.OnInputMoveRight(), token));

            // 回転：単発（指を離すまで再発火しない）
            Bind(_btnRotateCW,  new SingleFireStrategy(() => _controller?.OnInputRotateCW()));
            Bind(_btnRotateCCW, new SingleFireStrategy(() => _controller?.OnInputRotateCCW()));

            // ソフトドロップ：押しっぱなし中だけ高速落下
            Bind(_btnSoftDrop, new HoldStrategy(
                () => _controller?.OnInputSoftDrop(),
                () => _controller?.OnInputSoftDropEnd()
            ));
        }

        private static void Bind(Button? btn, IInputButtonStrategy strategy)
        {
            if (btn == null) return;
            var ib = btn.GetComponent<InputButton>() ?? btn.gameObject.AddComponent<InputButton>();
            ib.Setup(strategy);
        }
    }
}
#nullable disable
