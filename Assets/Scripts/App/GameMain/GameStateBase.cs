#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;

namespace App
{
    public enum GamePhase { Playing, Clearing, Launching, SkillExecuting, GameOver }

    // ─── Interface ────────────────────────────────────────────────────────────

    /// <summary>
    /// ゲームステートインターフェース（GoF Stateパターン）。
    /// GameMainController がこれを介してステートを扱う。
    /// </summary>
    public interface IGameState
    {
        GamePhase Phase       { get; }
        bool      AcceptsInput { get; }

        void OnEnter(CancellationToken ct);
        void OnExit();

        // PuyoBoard からのイベント
        void OnPairLocked();
        void OnChainCompleted(int chainCount, int clearedCount);
        void OnNextPairSpawned();
        void OnBoardGameOver();
    }

    // ─── Abstract Base ────────────────────────────────────────────────────────

    /// <summary>
    /// ステート共通基底クラス。各ステートは必要なメソッドのみオーバーライドする
    /// （GoF Template Methodパターン）。
    /// </summary>
    public abstract class GameStateBase : IGameState
    {
        protected readonly GameContext _ctx;

        protected GameStateBase(GameContext ctx) => _ctx = ctx;

        public abstract GamePhase Phase        { get; }
        public virtual  bool      AcceptsInput => false;

        public virtual void OnEnter(CancellationToken ct) { }
        public virtual void OnExit() { }

        public virtual void OnPairLocked()             { }
        public virtual void OnChainCompleted(int chainCount, int clearedCount) { }
        public virtual void OnNextPairSpawned()        { }
        public virtual void OnBoardGameOver()
            => _ctx.ChangeState(new GameOverState(_ctx));  // デフォルト：全ステートで共通
    }
}
#nullable disable
