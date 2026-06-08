using UnityEngine;
using UnityEngine.InputSystem;

namespace App
{
    /// <summary>
    /// キーボード入力を GameMainController の入力メソッドに変換するブリッジ。
    /// GameMainController から Update / キーボード依存を完全に分離する。
    /// Adapter パターン（キーボード → IAutoPlayTarget）。
    /// GameMainScene.OnInitialize() から Initialize() を呼んで使う。
    /// </summary>
    public sealed class KeyboardInputBridge : MonoBehaviour
    {
        private GameMainController _controller;

        private bool _softDropActive;

        public void Initialize(GameMainController controller)
        {
            _controller = controller;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // ゲームオーバー中: R キーでリスタート
            if (_controller.IsGameOver)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                    _controller.RestartGame();
                return;
            }

            // ─── ぷよ操作 ─────────────────────────────────────────
            if (keyboard.leftArrowKey.wasPressedThisFrame  || keyboard.aKey.wasPressedThisFrame)  _controller.OnInputMoveLeft();
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)  _controller.OnInputMoveRight();

            if (keyboard.xKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame
                || keyboard.upArrowKey.wasPressedThisFrame)                                        _controller.OnInputRotateCW();
            if (keyboard.zKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame)           _controller.OnInputRotateCCW();

            // ─── 天撃ボタン（ストック発動）───────────────────────────
            if (keyboard.spaceKey.wasPressedThisFrame)
                _controller.OnInputTengeki();

            // ─── ソフトドロップ（押しっぱなし判定）──────────────────
            var wantsSoftDrop = keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed;
            if (wantsSoftDrop && !_softDropActive)
            {
                _softDropActive = true;
                _controller.OnInputSoftDrop();
            }
            else if (!wantsSoftDrop && _softDropActive)
            {
                _softDropActive = false;
                _controller.OnInputSoftDropEnd();
            }
        }
    }
}
