using System;
using System.Collections.Generic;
using App.Puyo;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace App
{
    /// <summary>
    /// ゲーム進行を管理する中枢クラス。
    /// GoF State パターンで Playing / Clearing / Launching / GameOver を制御する。
    ///
    /// 責任：ステートマシン管理・イベント配線・入力受付口の提供
    /// 委譲：
    ///   キーボード入力  → KeyboardInputBridge
    ///   画面表示        → GameOverHUD
    ///   デバッグ API    → GameMainController.Debug.cs（partial / #if UNITY_EDITOR）
    /// </summary>
    public sealed partial class GameMainController : MonoBehaviour, IDisposable, IAutoPlayTarget
    {
        // ─── Inspector 参照 ──────────────────────────────────────
        [SerializeField] private GameModeConfig _config;

        // ─── 内部状態 ────────────────────────────────────────────
        private Game2DContents _contents;
        private ViewManager?   _viewMng;
        private GameContext    _ctx;
        private IGameState     _state;
        private HoldSystem     _holdSystem;

        // ─── 公開プロパティ ───────────────────────────────────────
        /// <summary>現在入力を受け付けられるか（IAutoPlayTarget / KeyboardInputBridge 用）</summary>
        public bool IsGameOver   { get; private set; }
        public bool AcceptsInput => _state?.AcceptsInput ?? false;

        // ─── 初期化 ──────────────────────────────────────────────

        public UniTask InitializeAsync(Game2DContents contents, ViewManager viewMng)
        {
            _contents   = contents;
            _viewMng    = viewMng;
            _holdSystem = new HoldSystem(destroyCancellationToken);

            _ctx = new GameContext(contents, viewMng, _config, ChangeState);

            // PuyoBoard イベントを購読
            var board = _contents.PuyoBoard;
            board.OnPairLocked       += OnBoardPairLocked;
            board.OnChainCompleted   += OnBoardChainCompleted;
            board.OnNextQueueChanged += OnBoardNextQueueChanged;
            board.OnGameOver         += OnBoardGameOver;

            // パチンコゾーン → 保留システム
            _contents.PachinkoController.OnHesoEntered += OnHesoEntered;

            // 保留システム → View
            _holdSystem.OnTechActivated += OnTechActivated;
            if (_contents.HoldDisplay != null)
            {
                _holdSystem.OnHoldAdded    += _contents.HoldDisplay.OnHoldAdded;
                _holdSystem.OnHoldShifted  += _contents.HoldDisplay.OnHoldShifted;
                _holdSystem.OnHoldConsumed += _contents.HoldDisplay.OnHoldConsumed;
            }

            // NEXTぷよ表示
            if (_contents.NextPuyoDisplay != null)
            {
                _contents.NextPuyoDisplay.Initialize(board.ColorSprites);
                board.OnNextQueueChanged += _contents.NextPuyoDisplay.Refresh;
            }

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            if (_contents == null) return;

            var board = _contents.PuyoBoard;
            board.OnPairLocked       -= OnBoardPairLocked;
            board.OnChainCompleted   -= OnBoardChainCompleted;
            board.OnNextQueueChanged -= OnBoardNextQueueChanged;
            board.OnGameOver         -= OnBoardGameOver;

            _contents.PachinkoController.OnHesoEntered -= OnHesoEntered;

            _holdSystem.OnTechActivated -= OnTechActivated;
            if (_contents.HoldDisplay != null)
            {
                _holdSystem.OnHoldAdded    -= _contents.HoldDisplay.OnHoldAdded;
                _holdSystem.OnHoldShifted  -= _contents.HoldDisplay.OnHoldShifted;
                _holdSystem.OnHoldConsumed -= _contents.HoldDisplay.OnHoldConsumed;
            }

            if (_contents.NextPuyoDisplay != null)
                board.OnNextQueueChanged -= _contents.NextPuyoDisplay.Refresh;
        }

        // ─── ゲーム開始・リスタート ──────────────────────────────

        public void StartGame()
        {
            IsGameOver = false;
            _contents.PuyoBoard.Initialize();
            ChangeState(new PlayingState(_ctx));
        }

        public void RestartGame()
            => UnitySceneManager.LoadScene(UnitySceneManager.GetActiveScene().name);

        // ─── ステートマシン ───────────────────────────────────────

        private void ChangeState(IGameState next)
        {
            _state?.OnExit();
            _state = next;
            _state.OnEnter(destroyCancellationToken);
            IsGameOver = _state.Phase == GamePhase.GameOver;
        }

        // ─── PuyoBoard イベントハンドラ ──────────────────────────

        private void OnBoardPairLocked()
            => _state?.OnPairLocked();

        private void OnBoardChainCompleted(int chainCount, int clearedCount)
            => _state?.OnChainCompleted(chainCount, clearedCount);

        private void OnBoardNextQueueChanged(IReadOnlyList<PuyoPairColors> _)
            => _state?.OnNextPairSpawned();

        private void OnBoardGameOver()
            => _state?.OnBoardGameOver();

        // ─── パチンコゾーン / 保留 ────────────────────────────────

        private void OnHesoEntered()
            => _holdSystem.AddHold(SelectHoldType());

        private void OnTechActivated(HoldType holdType)
        {
            // TODO: プロレス技の演出・ぷよ消し処理
            Debug.Log($"[GameMainController] 技発動！ type={holdType}（TODO: ぷよ消し）");
        }

        /// <summary>へそ入賞時の保留種別を抽選する。</summary>
        private HoldType SelectHoldType()
        {
            if (UnityEngine.Random.value < _config.BlackHoldProbability)
                return HoldType.Black;
            return (HoldType)UnityEngine.Random.Range(0, 4); // Red〜Blue
        }

        // ─── 入力公開メソッド（IAutoPlayTarget / PuyoInputView から呼ぶ）────

        public void OnInputMoveLeft()    { if (AcceptsInput) _contents.PuyoBoard.ActivePair?.MoveLeft(); }
        public void OnInputMoveRight()   { if (AcceptsInput) _contents.PuyoBoard.ActivePair?.MoveRight(); }
        public void OnInputRotateCW()    { if (AcceptsInput) _contents.PuyoBoard.ActivePair?.RotateCW(); }
        public void OnInputRotateCCW()   { if (AcceptsInput) _contents.PuyoBoard.ActivePair?.RotateCCW(); }
        public void OnInputSoftDrop()    { if (AcceptsInput) _contents.PuyoBoard.ActivePair?.BeginSoftDrop(); }
        public void OnInputSoftDropEnd() { if (AcceptsInput) _contents.PuyoBoard.ActivePair?.EndSoftDrop(); }
    }
}
