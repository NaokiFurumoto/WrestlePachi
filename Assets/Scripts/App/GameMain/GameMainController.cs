using System;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;
using UnityEngine.InputSystem;

namespace App
{
    /// <summary>
    /// ゲーム進行を管理する中枢クラス。
    /// PuyoBoard・BallLauncher・PachinkoZone のイベントを受け取り、
    /// State パターンで画面遷移・スコア・技発動を制御する。
    /// </summary>
    public sealed class GameMainController : MonoBehaviour, IDisposable
    {
        // ─── 内部参照 ────────────────────────────────────────────
        private Game2DContents  _contents;
        private ViewManager     _viewMng;
        private bool            _keyboardSoftDropActive;

        // ─── 初期化 ──────────────────────────────────────────────

        /// <summary>
        /// GameMainScene.OnInitialize() から呼ばれる。
        /// </summary>
        public UniTask InitializeAsync(Game2DContents contents, ViewManager viewMng)
        {
            _contents = contents;
            _viewMng  = viewMng;

            // PuyoBoard の連鎖完了イベントを購読
            _contents.PuyoBoard.OnChainCompleted    += OnChainCompleted;

            // NEXTぷよ更新イベントを購読（PuyoBoard.StartGame より前に登録することで初回も受け取る）
            if (_contents.NextPuyoDisplay != null)
            {
                _contents.NextPuyoDisplay.Initialize(_contents.PuyoBoard.ColorSprites);
                _contents.PuyoBoard.OnNextQueueChanged += _contents.NextPuyoDisplay.Refresh;
            }

            // パチンコゾーンのへそ入賞イベントを購読
            _contents.PachinkoZone.OnHesoEntered    += OnHesoEntered;

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// GameMainScene.OnRelease() から呼ばれる。
        /// </summary>
        public void Dispose()
        {
            if (_contents == null) return;

            _contents.PuyoBoard.OnChainCompleted   -= OnChainCompleted;
            if (_contents.NextPuyoDisplay != null)
                _contents.PuyoBoard.OnNextQueueChanged -= _contents.NextPuyoDisplay.Refresh;
            _contents.PachinkoZone.OnHesoEntered   -= OnHesoEntered;
        }

        // ─── ゲーム開始 ──────────────────────────────────────────

        /// <summary>
        /// フェードアウト完了後に GameMainScene から呼ばれる。
        /// ゲームを開始状態にする。
        /// </summary>
        public void StartGame()
        {
            // TODO: State を PlayingState に遷移
            _contents.PuyoBoard.Initialize();
        }

        private void Update()
        {
            if (_contents == null || _contents.PuyoBoard == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                OnInputMoveLeft();

            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                OnInputMoveRight();

            if (keyboard.xKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                OnInputRotateCW();

            if (keyboard.zKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame)
                OnInputRotateCCW();

            var wantsSoftDrop = keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed;
            if (wantsSoftDrop)
            {
                _keyboardSoftDropActive = true;
                OnInputSoftDrop();
            }
            else if (_keyboardSoftDropActive)
            {
                _keyboardSoftDropActive = false;
                OnInputSoftDropEnd();
            }
        }

        // ─── イベントハンドラ ────────────────────────────────────

        /// <summary>
        /// ぷよの連鎖が完了したとき（PuyoBoard から通知）。
        /// 連鎖数に応じてパチンコ玉を発射する。
        /// </summary>
        private void OnChainCompleted(int chainCount)
        {
            // TODO: State チェック・コンボ表示更新
            Debug.Log($"[GameMainController] 連鎖完了: {chainCount}連鎖");
            _contents.BallLauncher.LaunchAsync(chainCount, destroyCancellationToken).Forget();
        }

        /// <summary>
        /// パチンコ玉がへそに入賞したとき（PachinkoZone から通知）。
        /// </summary>
        private void OnHesoEntered()
        {
            // TODO: 技発動（TechSystem）
            Debug.Log("[GameMainController] へそ入賞");
        }

        // ─── 入力受付（PuyoInputView から呼ぶ） ──────────────────

        /// <summary>タッチ入力を PuyoPair に転送する</summary>
        public void OnInputMoveLeft()   => _contents.PuyoBoard.ActivePair?.MoveLeft();
        public void OnInputMoveRight()  => _contents.PuyoBoard.ActivePair?.MoveRight();
        public void OnInputRotateCW()   => _contents.PuyoBoard.ActivePair?.RotateCW();
        public void OnInputRotateCCW()  => _contents.PuyoBoard.ActivePair?.RotateCCW();
        public void OnInputSoftDrop()   => _contents.PuyoBoard.ActivePair?.BeginSoftDrop();
        public void OnInputSoftDropEnd()=> _contents.PuyoBoard.ActivePair?.EndSoftDrop();
    }
}
