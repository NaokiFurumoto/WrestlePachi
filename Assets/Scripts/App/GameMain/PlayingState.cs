#nullable enable
using System.Threading;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 通常プレイ中ステート。
    /// ぷよが落下中で、プレイヤーの入力を受け付ける。
    /// </summary>
    public sealed class PlayingState : GameStateBase
    {
        public PlayingState(GameContext ctx) : base(ctx) { }

        public override GamePhase Phase        => GamePhase.Playing;
        public override bool      AcceptsInput => true;

        public override void OnEnter(CancellationToken ct)
            => Debug.Log("[State] → Playing");

        /// <summary>ペアが着地したら連鎖処理ステートへ</summary>
        public override void OnPairLocked()
            => _ctx.ChangeState(new ClearingState(_ctx));
    }
}
#nullable disable
