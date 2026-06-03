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
        public void Debug_LaunchBalls(int count)                            => _contents?.BallLauncher?.LaunchAsync(count, destroyCancellationToken).Forget();
        public void Debug_ClearAllBalls()                                   => _contents?.BallLauncher?.Debug_ClearAllBalls();
        public void Debug_ForceChainCompleted(int chainCount, int cleared)  => _state?.OnChainCompleted(chainCount, cleared);
        public void Debug_ForceGameOver()                                   => _state?.OnBoardGameOver();

        // ─── GameModeConfig ───────────────────────────────────────
        public void Debug_SetBlackProbability(float value) => _config?.Debug_SetBlackHoldProbability(value);
        public void Debug_SetBallsPerPuyo(float value)     => _config?.Debug_SetBallsPerPuyo(value);

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

        // ─── エディタ専用フィールド ───────────────────────────────
        private AutoPlayAgent _autoPlayAgent;
    }
}
#endif
