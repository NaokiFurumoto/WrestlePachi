#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// パチンコ玉発射中ステート。
    /// BallLauncher が非同期で動作する間、ぷよの落下は継続して入力を受け付ける。
    /// </summary>
    public sealed class LaunchingState : GameStateBase
    {
        private readonly int _chainCount;
        private readonly int _clearedCount;

        public LaunchingState(GameContext ctx, int chainCount, int clearedCount) : base(ctx)
        {
            _chainCount   = chainCount;
            _clearedCount = clearedCount;
        }

        public override GamePhase Phase        => GamePhase.Launching;
        public override bool      AcceptsInput => true;

        public override void OnEnter(CancellationToken ct)
        {
            var ballCount = _ctx.Config.CalcBallCount(_clearedCount);
            Debug.Log($"[State] → Launching ({_chainCount}連鎖, {_clearedCount}個消え → {ballCount}球)");
            // 玉発射は fire-and-forget。ぷよの落下と同時進行する。
            _ctx.Contents.BallLauncher.LaunchAsync(ballCount, ct).Forget();
        }

        /// <summary>次のペアがスポーンしたらプレイ再開</summary>
        public override void OnNextPairSpawned()
            => _ctx.ChangeState(new PlayingState(_ctx));

        /// <summary>Launching 中に次のペアも着地すれば連鎖処理へ</summary>
        public override void OnPairLocked()
            => _ctx.ChangeState(new ClearingState(_ctx));
    }
}
#nullable disable
