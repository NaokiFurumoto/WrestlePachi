#nullable enable
using System.Threading;
using App.Skills;
using Cysharp.Threading.Tasks;

namespace App
{
    /// <summary>
    /// スキル演出・消去処理中ステート（GoF State パターン）。
    /// 入力と落下をブロックし、スキル完了後に自律的に PlayingState へ戻る。
    /// </summary>
    public sealed class SkillExecutingState : GameStateBase
    {
        private readonly ITechSkill               _skill;
        private readonly UniTaskCompletionSource  _completionSource;

        public SkillExecutingState(
            GameContext              ctx,
            ITechSkill               skill,
            UniTaskCompletionSource  completionSource) : base(ctx)
        {
            _skill             = skill;
            _completionSource  = completionSource;
        }

        public override GamePhase Phase        => GamePhase.SkillExecuting;
        public override bool      AcceptsInput => false;

        // PairLocked / ChainCompleted / NextPairSpawned はスキル中に発火しても無視
        // （PauseGravity でペアは着地しないため、これらは発火しない想定だが念のため）

        public override void OnEnter(CancellationToken ct)
        {
            _ctx.Contents.PuyoBoard.ActivePair?.PauseGravity();
            RunAsync(ct).Forget();
        }

        private async UniTaskVoid RunAsync(CancellationToken ct)
        {
            // OnEnter のコールスタックを抜けてから実行（再入防止）
            await UniTask.Yield(cancellationToken: ct);
            try
            {
                await _skill.ExecuteAsync(_ctx, ct);
            }
            finally
            {
                _ctx.Contents.PuyoBoard.ActivePair?.ResumeGravity();
                // HoldSystem の await を解除する（キャンセル時も必ず呼ぶ）
                _completionSource.TrySetResult();
                if (!ct.IsCancellationRequested)
                    _ctx.ChangeState(new PlayingState(_ctx));
            }
        }
    }
}
#nullable disable
