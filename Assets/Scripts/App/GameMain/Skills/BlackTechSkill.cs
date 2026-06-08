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
        public bool HasCutIn      => false; // 演出なし・即時実行

        /// <summary>黒保留は常に発動可能（お邪魔予約はいつでも有効）。</summary>
        public bool CanExecute(GameContext ctx) => true;

        public UniTask ExecuteAsync(GameContext ctx, CancellationToken ct)
        {
            ctx.Contents.PuyoBoard.AddPendingOjama(ctx.Config.OjamaCountPerBlackTech);
            return UniTask.CompletedTask;
        }
    }
}
