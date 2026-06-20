#if UNITY_EDITOR
using App.Puyo;
using Cysharp.Threading.Tasks;

namespace App
{
    /// <summary>
    /// GameMainController のデバッグ API（エディタ専用）。
    /// partial class により本番コードを汚染しない。
    /// IDebugSection 実装クラスからのみ呼ぶこと。
    /// </summary>
    public sealed partial class GameMainController
    {
        // ─── 状態参照 ─────────────────────────────────────────────
        public GamePhase      Debug_CurrentPhase   => _state?.Phase ?? GamePhase.Playing;
        public GameModeConfig Debug_GameModeConfig => _config;
        public bool           Debug_IsAutoPlaying  => _autoPlayAgent?.IsRunning ?? false;

        public HoldType?[] Debug_GetHoldSlots()
            => _holdSystem?.Debug_GetSlots() ?? System.Array.Empty<HoldType?>();

        // ─── ゲーム操作 ───────────────────────────────────────────
        public void Debug_AddHold(HoldType type)                            => _holdSystem?.AddHold(type);
        public void Debug_ForceHesoEntry()                                  => OnHesoEntered();
        public void Debug_LaunchBalls(int count)                            => _contents?.BallLauncher?.LaunchAsync(count).Forget();
        public void Debug_ClearAllBalls()                                   => _contents?.BallLauncher?.Debug_ClearAllBalls();
        public void Debug_ForceChainCompleted(int chainCount, int cleared)
        {
            if (chainCount >= 2)
                _comboView?.Show(chainCount, _contents.PuyoBoard.LastChainCentroidWorld);
            _state?.OnChainCompleted(chainCount, cleared);
        }
        public void Debug_ForceGameOver()                                   => _state?.OnBoardGameOver();
        public void Debug_ForceGameClear()                                  => ChangeState(new GameClearState(_ctx));
        public void Debug_InstantKillEnemy()                                => _enemy?.InstantKill();

        public (int index, int total, int hp, int maxHp) Debug_GetEnemyInfo()
        {
            if (_enemy == null) return (-1, 0, 0, 0);
            return (_enemy.EnemyIndex, _enemy.TotalCount, _enemy.CurrentHp, _enemy.MaxHp);
        }

        // ─── GameModeConfig ───────────────────────────────────────
        public void Debug_SetBlackProbability(float value)           => _config?.Debug_SetBlackHoldProbability(value);
        public void Debug_SetBallsPerPuyo(float value)               => _config?.Debug_SetBallsPerPuyo(value);
        public void Debug_SetRainbowProbabilityDenominator(int value) => _config?.Debug_SetRainbowProbabilityDenominator(value);

        /// <summary>保留・ぷよの色バリアントをゲーム中に切り替える（4 or 5）。</summary>
        public void Debug_SetColorVariant(int count)
        {
            _config?.Debug_SetColorVariant(count);
            _contents?.PuyoBoard?.SetColorCount(count);
        }

        // ─── 自動プレイ ───────────────────────────────────────────
        public void Debug_StartAutoPlay(int thinkMs = 300, int moveMs = 100)
        {
            _autoPlayAgent ??= new AutoPlayAgent(this, _contents.PuyoBoard);
            _autoPlayAgent.ThinkDelayMs = thinkMs;
            _autoPlayAgent.MoveDelayMs  = moveMs;
            _autoPlayAgent.Start();
        }

        public void Debug_StopAutoPlay() => _autoPlayAgent?.Stop();

        public void Debug_FillBoard(int rows = 6) => _contents?.PuyoBoard?.Debug_FillBoard(rows);

        public void Debug_ShowCombo(int chainCount)
        {
            if (_comboView == null)
            {
                UnityEngine.Debug.LogWarning("[Debug] _comboView が null です。_comboViewPrefab のアサインを確認してください。");
                return;
            }
            if (chainCount >= 2) GameSys.AppSound.PlayChain();
            // デバッグ時は LastChainCentroidWorld が未設定のため盤面中央を使う
            var boardCenter = _contents?.PuyoBoard != null
                ? _contents.PuyoBoard.transform.position + new UnityEngine.Vector3(2.5f, 5f)
                : UnityEngine.Vector3.zero;
            _comboView.Show(chainCount, boardCenter);
        }

        // ─── エディタ専用フィールド ───────────────────────────────
        private AutoPlayAgent _autoPlayAgent;
    }
}
#endif
