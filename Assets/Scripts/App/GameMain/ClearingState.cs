#nullable enable
using System.Threading;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 連鎖処理中ステート。
    /// ぷよの消去・落下アニメーション中で入力をブロックする。
    /// </summary>
    public sealed class ClearingState : GameStateBase
    {
        public ClearingState(GameContext ctx) : base(ctx) { }

        public override GamePhase Phase        => GamePhase.Clearing;
        public override bool      AcceptsInput => false;

        public override void OnEnter(CancellationToken ct)
            => Debug.Log("[State] → Clearing");

        /// <summary>連鎖が1回以上あれば玉発射ステートへ</summary>
        public override void OnChainCompleted(int chainCount, int clearedCount)
            => _ctx.ChangeState(new LaunchingState(_ctx, chainCount, clearedCount));

        /// <summary>
        /// 連鎖なしの場合、OnChainCompleted は発火せず直接 SpawnNextPair が呼ばれる。
        /// 次のペアがスポーンしたらプレイ再開。
        /// </summary>
        public override void OnNextPairSpawned()
            => _ctx.ChangeState(new PlayingState(_ctx));
    }
}
#nullable disable
