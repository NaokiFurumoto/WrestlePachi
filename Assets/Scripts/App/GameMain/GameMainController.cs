using System;
using System.Collections.Generic;
using System.Threading;
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
        [SerializeField] private HoldBeamEffect?      _holdBeamEffect;

        // ─── 内部状態 ────────────────────────────────────────────
        private Game2DContents              _contents;
        private TengekiButton?              _tengekiButton;
        private GameContext                 _ctx;
        private IGameState                  _state;
        private HoldSystem                  _holdSystem;
        private CancellationTokenSource?    _stateCts;
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
            _contents      = contents;
            _tengekiButton = contents.TengekiButton;
            if (_tengekiButton != null) _tengekiButton.OnClicked += OnInputTengeki;
            _holdSystem    = new HoldSystem(destroyCancellationToken);

            _ctx = new GameContext(contents, viewMng, _config, ChangeState);
            _ctx.Enemy         = _enemy;
            _ctx.RestartGame   = RestartGame;

            // 敵撃破イベント
            if (_enemy != null)
                _enemy.OnDefeated += OnEnemyDefeated;

            // ストックシステム初期化
            _skillStockSystem = new SkillStockSystem();
            _skillStockSystem.OnStockChanged += _ => UpdateTengekiButtonGlow();

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
            _stateCts?.Cancel();
            _stateCts?.Dispose();
            _stateCts = null;

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
            ApplyStageConfig(_enemy?.EnemyIndex ?? 0, initBoard: true);
            ChangeState(new PlayingState(_ctx));
            var hud = ViewManager.GetView<GameMainHudView>();
            if (hud != null) hud.OptionClicked += OnInputOption;
            if (_enemy != null) hud?.SetEnemy(_enemy);
            hud?.SetTime(_remainingTime);
            if (_enemy != null) hud?.SetStage(_enemy.EnemyIndex + 1, _enemy.TotalCount);
        }

        // initBoard=true はゲーム開始時のみ。敵切り替え時は false（盤面リセット禁止）
        private void ApplyStageConfig(int stageIndex, bool initBoard = false)
        {
            var stage = StageConfigList.Instance?.Get(stageIndex);
            _ctx.CurrentStage   = stage;
            _remainingTime      = stage?.TimeLimit ?? _config.TimeLimitSeconds;
            _lastNotifiedSecond = -1;
            var colorCount    = stage?.ColorCount      ?? _config.ColorVariant;
            var fallSpeed     = stage?.PuyoFallSpeed   ?? 1f;
            var garbageRows   = stage?.GarbageInitialRows ?? 0;
            if (initBoard)
                _contents.PuyoBoard.Initialize(colorCount, garbageRows);
            else
                _contents.PuyoBoard.SetColorCount(colorCount);
            _contents.PuyoBoard.SetFallSpeedMultiplier(fallSpeed);
        }

        public void RestartGame()
        {
            var sceneName = UnitySceneManager.GetActiveScene().name;
            if (SceneManager.isValid)
                SceneManager.Instance.TransitScene(sceneName);
            else
                UnitySceneManager.LoadScene(sceneName);
        }

        // ─── タイマー更新 ─────────────────────────────────────────

        private void Update()
        {
            var timeLimit = _ctx.CurrentStage?.TimeLimit ?? _config.TimeLimitSeconds;
            if (IsGameOver || timeLimit <= 0f) return;

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
            _stateCts?.Cancel();
            _stateCts?.Dispose();
            _stateCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            _state?.OnExit();
            _state = next;
            _state.OnEnter(_stateCts.Token);
            IsGameOver = _state.Phase == GamePhase.GameOver;
            UpdateTengekiButtonGlow();
        }

        // ストックあり + PlayingState のときだけボタンを光らせ、Bob ループも連動させる
        private void UpdateTengekiButtonGlow()
        {
            var showGlow = _skillStockSystem.HasStock && _state is PlayingState;
            _tengekiButton?.SetStockGlow(showGlow ? _skillStockSystem.Current : null);
            _tengekiButton?.SetBobActive(showGlow);
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
            if (_enemy == null) return;

            var nextIndex    = _enemy.EnemyIndex + 1;
            var hasNextStage = nextIndex < _enemy.TotalCount;
            Action? onNext   = hasNextStage ? () => AdvanceToNextStage(nextIndex) : null;
            ChangeState(new EnemyDyingState(_ctx, hasNextStage, onNext));
        }

        // ステージクリア後に「次のステージへ」を押したときに呼ぶ
        private void AdvanceToNextStage(int nextIndex)
        {
            if (_enemy == null) return;
            _enemy.SetEnemy(nextIndex);
            ApplyStageConfig(nextIndex, initBoard: true);
            var hud = ViewManager.GetView<GameMainHudView>();
            hud?.SetEnemy(_enemy);
            hud?.SetStage(nextIndex + 1, _enemy.TotalCount);
            hud?.SetTime(_remainingTime);
            ChangeState(new PlayingState(_ctx));
        }

        // ─── 入力公開メソッド（IAutoPlayTarget / PuyoInputView から呼ぶ）────

        public void OnInputMoveLeft()    { if (AcceptsInput) _contents.PuyoBoard.ActivePair?.MoveLeft(); }
        public void OnInputMoveRight()   { if (AcceptsInput) _contents.PuyoBoard.ActivePair?.MoveRight(); }
        public void OnInputRotateCW()    { if (AcceptsInput) _contents.PuyoBoard.ActivePair?.RotateCW(); }
        public void OnInputRotateCCW()   { if (AcceptsInput) _contents.PuyoBoard.ActivePair?.RotateCCW(); }
        public void OnInputSoftDrop()    { if (AcceptsInput) _contents.PuyoBoard.ActivePair?.BeginSoftDrop(); }
        public void OnInputSoftDropEnd() { if (AcceptsInput) _contents.PuyoBoard.ActivePair?.EndSoftDrop(); }
        public void OnInputOption()      { if (_state?.CanPause ?? false) ChangeState(new PauseState(_ctx)); }
    }
}
