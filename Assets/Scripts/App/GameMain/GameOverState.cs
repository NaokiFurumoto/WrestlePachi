#nullable enable
using System.Threading;
using UnityEngine;

namespace App
{
    /// <summary>
    /// ゲームオーバーステート。
    /// スポーン位置が埋まったときに遷移し、入力をすべてブロックする。
    /// </summary>
    public sealed class GameOverState : GameStateBase
    {
        public GameOverState(GameContext ctx) : base(ctx) { }

        public override GamePhase Phase        => GamePhase.GameOver;
        public override bool      AcceptsInput => false;

        public override void OnEnter(CancellationToken ct)
        {
            Debug.Log("[State] → GameOver");
            // TODO: ゲームオーバー演出・リトライUI表示
        }

        // ゲームオーバー後はいかなるイベントも無視する
        public override void OnPairLocked()         { }
        public override void OnChainCompleted(int chainCount, int clearedCount) { }
        public override void OnNextPairSpawned()    { }
        public override void OnBoardGameOver()      { }
    }
}
#nullable disable
