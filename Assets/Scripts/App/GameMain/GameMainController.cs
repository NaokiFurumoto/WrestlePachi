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


        // ─── 内部状態 ────────────────────────────────────────────
        private Game2DContents   _contents;
        private ViewManager?     _viewMng;
        private GameContext      _ctx;
        private IGameState       _state;
        private HoldSystem       _holdSystem;
        private TechSkillManager _techSkillManager;
        private SkillStockSystem _skillStockSystem;
        private bool             _isSimulatorMode;

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
            _techSkillManager.OnSkillExecuted += BloomEffectController.PlaySkill;

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

            _techSkillManager.OnSkillExecuted -= BloomEffectController.PlaySkill;

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
            _contents.PuyoBoard.Initialize(_config.ColorVariant);
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
        {
            if (_isSimulatorMode) return;
            _holdSystem.AddHold(SelectHoldType());
        }

        private void OnPocketEntered()
        {
            if (_isSimulatorMode) return;
            _contents.BallLauncher.LaunchAsync(1, destroyCancellationToken).Forget();
        }

        /// <summary>
        /// シミュレーターモードを切り替える。
        /// ON にするとぷよが停止し、へそ入賞でも保留が追加されなくなる。
        /// </summary>
        public void SetSimulatorMode(bool active)
        {
            _isSimulatorMode = active;
            if (active) _contents.PuyoBoard.Suspend();
        }

        /// <summary>
        /// HoldSystem から await される非同期ハンドラ。
        /// 返した UniTask が完了するまで HoldSystem は次の保留へ進まない。
        /// </summary>
        private UniTask OnTechActivatedAsync(HoldType holdType)
        {
            // PlayingState 以外（連鎖中・玉発射中・スキル実行中）はストックに積んで即完了
            if (_state is not PlayingState)
            {
                if (!_skillStockSystem.TryAddStock(holdType))
                    Debug.LogWarning($"[GameMainController] ストックが満杯のため HoldType={holdType} を破棄");
                return UniTask.CompletedTask;
            }
            return _techSkillManager.StartSkillAsync(holdType, _ctx, _skillStockSystem, destroyCancellationToken);
        }

        /// <summary>天撃ボタン入力：虹PUSH待ち中はRainbowInputSourceを完了させる。それ以外はストック発動。</summary>
        public void OnInputTengeki()
        {
            // 虹PUSH待ち中 → ビュー上のボタンと同じ扱いで TCS を完了させる
            if (_ctx.RainbowInputSource != null)
            {
                _ctx.RainbowInputSource.TrySetResult();
                return;
            }
            if (_state is not PlayingState) return;
            if (_skillStockSystem.TryConsumeStock(out var holdType))
                _techSkillManager.StartSkillAsync(holdType, _ctx, _skillStockSystem, destroyCancellationToken).Forget();
        }

        /// <summary>ストックMAX到達時：全ストック消費して盤面を全消しする。</summary>
        private async UniTaskVoid OnSkillStockMaxAsync(CancellationToken ct)
        {
            _skillStockSystem.ConsumeAll();
            await _ctx.Contents.PuyoBoard.ClearAllPuyosAsync(ct);
        }

        /// <summary>へそ入賞時の保留種別を抽選する。</summary>
        private HoldType SelectHoldType()
        {
            // 虹保留チェック（1/RainbowProbabilityDenominator の確率）
            var denom = _config.RainbowProbabilityDenominator;
            if (denom > 0 && UnityEngine.Random.Range(0, denom) == 0)
                return HoldType.Rainbow;

            if (UnityEngine.Random.value < _config.BlackHoldProbability)
                return HoldType.Black;

            return (HoldType)UnityEngine.Random.Range(0, _config.ColorVariant); // Red〜Blue or Red〜Purple
        }

        /// <summary>保留追加時のハンドラ。虹保留なら バイブループを開始する。</summary>
        private void OnHoldSystemAdded(int index, HoldType holdType)
        {
            if (holdType != HoldType.Rainbow) return;
            // 多重起動防止
            _ctx.RainbowVibrationCts?.Cancel();
            _ctx.RainbowVibrationCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            VibrationLoopAsync(_ctx.RainbowVibrationCts.Token).Forget();
        }

        private void OnEnemyDefeated()
        {
            // TODO: クリア演出を実装する
            Debug.Log("[GameMainController] 敵を撃破！クリア");
        }

        private static async UniTaskVoid VibrationLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
#if UNITY_IOS || UNITY_ANDROID
                Handheld.Vibrate();
#endif
                await UniTask.Delay(500, cancellationToken: ct).SuppressCancellationThrow();
            }
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
