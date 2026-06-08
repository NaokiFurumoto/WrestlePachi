using System.Threading;
using App;
using App.Puyo;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;

namespace App.Skills
{
    /// <summary>
    /// 緑保留の技：ラリアット。
    /// カットイン後、最も密集した 2×2 エリアを回転順に消す。
    /// </summary>
    public sealed class GreenTechSkill : ITechSkill
    {
        private static readonly Vector2 ImageSize = new Vector2(905f, 530f);

        private readonly RuntimeAnimatorController _controller;

        public HoldType TargetType => HoldType.Green;
        public bool HasCutIn      => true;

        public GreenTechSkill(RuntimeAnimatorController controller)
        {
            _controller = controller;
        }

        /// <summary>盤面にぷよが1個以上あれば発動可。</summary>
        public bool CanExecute(GameContext ctx) => ctx.Contents.PuyoBoard.FindDensest2x2().x >= 0;

        public async UniTask ExecuteAsync(GameContext ctx, CancellationToken ct)
        {
            var board = ctx.Contents.PuyoBoard;
            var origin = board.FindDensest2x2();

            var handle = await ViewManager.PushViewAsync<SkillCutInView>(
                ViewKeys.SKILL_CUT_IN,
                new SkillCutInView.SkillCutInViewData { Controller = _controller, ImageSize = ImageSize });
            var cutInView = handle?.View as SkillCutInView;
            if (cutInView != null) await cutInView.PlayAttackAsync(ct);
            await ViewManager.PopViewAsync(handle); // アニメ完了後すぐスライドアウト

            // 回転順（左下→右下→右上→左上）に消去してラリアットの回転感を演出
            var cells = new[]
            {
                origin,
                new Vector2Int(origin.x + 1, origin.y),
                new Vector2Int(origin.x + 1, origin.y + 1),
                new Vector2Int(origin.x,     origin.y + 1),
            };
            var puyoCount = board.GetNonNullCountInCells(cells);
            foreach (var cell in cells)
            {
                await board.ClearCellBySkillAsync(cell, ct);
                await UniTask.Delay(30, cancellationToken: ct);
            }

            await board.DropFloatingAfterSkillAsync(ct);
            ctx.Enemy?.TakeDamage(DamageCalculator.CalcSkillDamage(puyoCount, 2.5f));
        }
    }
}
