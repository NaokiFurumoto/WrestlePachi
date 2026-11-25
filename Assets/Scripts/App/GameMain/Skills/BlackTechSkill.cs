using System.Threading;
using Cysharp.Threading.Tasks;

namespace App.Skills
{
    /// <summary>
    /// 黒保留の技。
    /// お邪魔ぷよをボードに予約し、次のペアがロックされたタイミングで降らせる。
    /// 敵スキル「黒保留威力」中は、数が増える演出（GameContext.PlayBlackPowerUp）を見せてから予約する。
    /// </summary>
    public sealed class BlackTechSkill : ITechSkill
    {
        public HoldType TargetType => HoldType.Black;
        public bool HasCutIn      => false; // 演出なし・即時実行

        /// <summary>黒保留は常に発動可能（お邪魔予約はいつでも有効）。</summary>
        public bool CanExecute(GameContext ctx) => true;

        public async UniTask ExecuteAsync(GameContext ctx, CancellationToken ct)
        {
            // 敵スキル「黒保留威力」が発動中ならそちらを優先する
            var baseCount = ctx.Config.OjamaCountPerBlackTech;
            var count     = ctx.BlackHoldOjamaCountOverride ?? baseCount;

            // 増えるときは、小悪魔が黒い玉をパンパンに膨らませて数が跳ね上がるのを見せてから降らせる
            if (count > baseCount && ctx.PlayBlackPowerUp != null)
            {
                var canceled = await ctx.PlayBlackPowerUp(baseCount, count, ct).SuppressCancellationThrow();
                if (canceled) return;
            }
            ctx.Contents.PuyoBoard.AddPendingOjama(count);
        }
    }
}
