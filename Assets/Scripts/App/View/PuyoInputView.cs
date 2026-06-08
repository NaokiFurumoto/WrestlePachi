#nullable enable
using GameSys;
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

        private GameMainController? _controller;

        // ─── ViewBase override ───────────────────────────────────────

        protected override void OnInitialize()
        {
            if (Data is InputViewData data)
                _controller = data.Controller;

            BindButtons();
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
