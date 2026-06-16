#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace App
{
    /// <summary>
    /// 敵のHP が 0 になった直後の演出ステート。
    /// 白フラッシュ → 敵のやられアクション → GameClearState へ遷移する。
    /// </summary>
    public sealed class EnemyDyingState : GameStateBase
    {
        private readonly bool    _hasNextStage;
        private readonly Action? _onNextStage;

        public EnemyDyingState(GameContext ctx, bool hasNextStage = false, Action? onNextStage = null) : base(ctx)
        {
            _hasNextStage = hasNextStage;
            _onNextStage  = onNextStage;
        }

        public override GamePhase Phase        => GamePhase.EnemyDying;
        public override bool      AcceptsInput => false;

        public override void OnBoardGameOver() { } // 演出中はゲームオーバーに遷移しない

        public override void OnEnter(CancellationToken ct)
            => RunAsync(ct).Forget();

        private async UniTaskVoid RunAsync(CancellationToken ct)
        {
            // 演出中に CT がキャンセルされてもゲームクリアへの遷移は必ず行う
            await ScreenEffectController.PlayDefeatImpact(ct).SuppressCancellationThrow();

            if (_ctx.Enemy != null)
                await _ctx.Enemy.PlayDefeatAsync(ct).SuppressCancellationThrow();

            _ctx.ChangeState(new GameClearState(_ctx, _hasNextStage, _onNextStage));
        }
    }
}
#nullable disable
