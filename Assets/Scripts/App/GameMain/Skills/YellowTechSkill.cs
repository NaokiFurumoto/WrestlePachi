using System.Threading;
using App;
using App.Puyo;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;

namespace App.Skills
{
    /// <summary>
    /// 黄保留の技：ストンピング。
    /// カットイン後、ランダムな5個のぷよをポンポンと消す。
    /// </summary>
    public sealed class YellowTechSkill : ITechSkill
    {
        private const int StompCount = 5;
        private static readonly Vector2 ImageSize = new Vector2(578f, 834f);

        private readonly RuntimeAnimatorController _controller;

        public HoldType TargetType => HoldType.Yellow;
        public bool HasCutIn      => true;

        public YellowTechSkill(RuntimeAnimatorController controller)
        {
            _controller = controller;
        }

        /// <summary>非お邪魔ぷよが1個以上あれば発動可。</summary>
        public bool CanExecute(GameContext ctx) => ctx.Contents.PuyoBoard.GetRandomPuyoPositions(1).Count > 0;

        public async UniTask ExecuteAsync(GameContext ctx, CancellationToken ct)
        {
            var board = ctx.Contents.PuyoBoard;
            var targets = board.GetRandomPuyoPositions(StompCount);

            var handle = await ViewManager.PushViewAsync<SkillCutInView>(
                ViewKeys.SKILL_CUT_IN,
                new SkillCutInView.SkillCutInViewData { Controller = _controller, ImageSize = ImageSize });
            var cutInView = handle?.View as SkillCutInView;
            if (cutInView != null) await cutInView.PlayAttackAsync(ct);
            await ViewManager.PopViewAsync(handle); // アニメ完了後すぐスライドアウト

            // ランダム5個をポンポン消去
            foreach (var cell in targets)
            {
                await board.ClearCellBySkillAsync(cell, ct);
                await UniTask.Delay(30, cancellationToken: ct);
            }

            await board.DropFloatingAfterSkillAsync(ct);
            ctx.Enemy?.TakeDamage(DamageCalculator.CalcSkillDamage(targets.Count, 2.5f));
        }
    }
}
