using System;
using System.Collections.Generic;
using App.Puyo;
using App.Skills;
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
    ///   保留/パチンコ系 → GameMainController.Hold.cs（partial）
    ///   デバッグ API    → GameMainController.Debug.cs（partial / #if UNITY_EDITOR）
    /// </summary>
    public sealed partial class GameMainController : MonoBehaviour, IDisposable, IAutoPlayTarget
    {
        // ─── Inspector 参照 ──────────────────────────────────────
        [SerializeField] private GameModeConfig _config;

        [Header("スキルカットイン AnimatorController")]
        [SerializeField] private RuntimeAnimatorController _redController;
        [SerializeField] private RuntimeAnimatorController _blueController;
        [SerializeField] private RuntimeAnimatorController _yellowController;
        [SerializeField] private RuntimeAnimatorController _greenController;
        [SerializeField] private RuntimeAnimatorController _purpleController;
        [SerializeField] private RuntimeAnimatorController _rainbowController;

        [Header("敵")]
        [SerializeField] private EnemyController? _enemy;

        [Header("演出")]
        [SerializeField] private GameEffectController _effectController;

        // ─── 内部状態 ────────────────────────────────────────────
        private Game2DContents   _contents;
        private GameContext      _ctx;
        private IGameState       _state;
        private HoldSystem       _holdSystem;
        private TechSkillManager _techSkillManager;
        private SkillStockSystem _skillStockSystem;
        private bool             _isSimulatorMode;
        private float            _remainingTime;
        private int              _lastNotifiedSecond = -1;

        // ─── 公開プロパティ ───────────────────────────────────────
        /// <summary>現在入力を受け付けられるか（IAutoPlayTarget / KeyboardInputBridge 用）</summary>
        public bool IsGameOver   { get; private set; }
        public bool AcceptsInput => _state?.AcceptsInput ?? false;
        public EnemyController?  Enemy => _enemy;

        // ─── 初期化 ──────────────────────────────────────────────

        public UniTask InitializeAsync(Game2DContents contents, ViewManager viewMng)
        {
            _contents   = contents;
            _holdSystem = new HoldSystem(destroyCancellationToken);

            _ctx = new GameContext(contents, viewMng, _config, ChangeState);
            _ctx.Enemy = _enemy;

            // 敵撃破イベント
            if (_enemy != null)
                _enemy.OnDefeated += OnEnemyDefeated;

            // ストックシステム初期化
            _skillStockSystem = new SkillStockSystem();
            _skillStockSystem.OnMaxReached += () => OnSkillStockMaxAsync(destroyCancellationToken).Forget();

            // スキルを登録。新技は ITechSkill 実装クラスをここに追加するだけでよい
            _techSkillManager = new TechSkillManager(new ITechSkill[]
            {
                new BlackTechSkill(),
                new RedTechSkill(_redController),
                new BlueTechSkill(_blueController),
                new YellowTechSkill(_yellowController),
                new GreenTechSkill(_greenController),
                new PurpleTechSkill(_purpleController),
                new RainbowTechSkill(_rainbowController),
            });

            // スキル実行時に Bloom 演出を再生（Singleton 経由でどこからでも呼べる）
            _techSkillManager.OnSkillExecuted += ScreenEffectController.PlaySkill;

            // ゲーム演出コントローラーを初期化
            if (_effectController != null)
                _effectController.Initialize(_contents.PuyoBoard, _techSkillManager);

            // PuyoBoard イベントを購読
            var board = _contents.PuyoBoard;
            board.OnPairLocked       += OnBoardPairLocked;
            board.OnChainCompleted   += OnBoardChainCompleted;
            board.OnNextQueueChanged += OnBoardNextQueueChanged;
            board.OnGameOver         += OnBoardGameOver;

            // パチンコゾーン → 保留システム
            _contents.PachinkoController.OnHesoEntered   += OnHesoEntered;
            _contents.PachinkoController.OnPocketEntered += OnPocketEntered;

            // 保留システム → View
            _holdSystem.OnTechActivated = OnTechActivatedAsync;
            _holdSystem.OnHoldAdded     += OnHoldSystemAdded;
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

            _contents.PachinkoController.OnHesoEntered   -= OnHesoEntered;
            _contents.PachinkoController.OnPocketEntered -= OnPocketEntered;

            if (_enemy != null)
                _enemy.OnDefeated -= OnEnemyDefeated;

            _techSkillManager.OnSkillExecuted -= ScreenEffectController.PlaySkill;

            if (_effectController != null)
                _effectController.Dispose(_contents.PuyoBoard, _techSkillManager);

            _holdSystem.OnTechActivated = null;
            _holdSystem.OnHoldAdded     -= OnHoldSystemAdded;
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
            _remainingTime      = _config.TimeLimitSeconds;
            _lastNotifiedSecond = -1;
            _contents.PuyoBoard.Initialize(_config.ColorVariant);
            ChangeState(new PlayingState(_ctx));
            var hud = ViewManager.GetView<GameMainHudView>();
            if (_enemy != null) hud?.SetEnemy(_enemy);
            hud?.SetTime(_remainingTime);
            if (_enemy != null) hud?.SetStage(_enemy.EnemyIndex + 1, _enemy.TotalCount);
        }

        public void RestartGame()
            => UnitySceneManager.LoadScene(UnitySceneManager.GetActiveScene().name);

        // ─── タイマー更新 ─────────────────────────────────────────

        private void Update()
        {
            if (IsGameOver || _config.TimeLimitSeconds <= 0f) return;

            _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);

            var currentSecond = Mathf.CeilToInt(_remainingTime);
            if (currentSecond != _lastNotifiedSecond)
            {
                _lastNotifiedSecond = currentSecond;
                ViewManager.GetView<GameMainHudView>()?.SetTime(_remainingTime);
            }

            if (_remainingTime <= 0f)
                _state?.OnBoardGameOver();
        }

        // ─── ステートマシン ───────────────────────────────────────

        private void ChangeState(IGameState next)
        {
            _state?.OnExit();
            _state = next;
            _state.OnEnter(destroyCancellationToken);
            IsGameOver = _state.Phase == GamePhase.GameOver;

            // PlayingState に遷移したタイミングでストックを1つ消費して発動
            if (_state is PlayingState && _skillStockSystem.TryConsumeStock(out var stockedHold))
                _techSkillManager.StartSkillAsync(stockedHold, _ctx, _skillStockSystem, destroyCancellationToken).Forget();
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

        private void OnEnemyDefeated()
        {
            // TODO: クリア演出を実装する
            Debug.Log("[GameMainController] 敵を撃破！クリア");
            if (_enemy != null)
                ViewManager.GetView<GameMainHudView>()?.SetStage(_enemy.EnemyIndex + 1, _enemy.TotalCount);
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
