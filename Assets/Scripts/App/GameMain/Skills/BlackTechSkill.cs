using System.Threading;
using Cysharp.Threading.Tasks;

namespace App.Skills
{
    /// <summary>
    /// 黒保留の技。
    /// お邪魔ぷよをボードに予約し、次のペアがロックされたタイミングで降らせる。
    /// </summary>
    public sealed class BlackTechSkill : ITechSkill
    {
        public HoldType TargetType => HoldType.Black;

        public UniTask ExecuteAsync(GameContext ctx, CancellationToken ct)
        {
            ctx.Contents.PuyoBoard.AddPendingOjama(ctx.Config.OjamaCountPerBlackTech);
            return UniTask.CompletedTask;
        }
    }
}
