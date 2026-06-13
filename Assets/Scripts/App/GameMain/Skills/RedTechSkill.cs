using System.Threading;
using App;
using App.Puyo;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;

namespace App.Skills
{
    /// <summary>
    /// 赤保留の技：ドロップキック。
    /// カットイン後、最も密集した横一列を左から右へ順番に消す。
    /// </summary>
    public sealed class RedTechSkill : ITechSkill
    {
        private static readonly Vector2 ImageSize = new Vector2(747f, 828f);

        private readonly RuntimeAnimatorController _controller;

        public HoldType TargetType => HoldType.Red;
        public bool HasCutIn      => true;

        public RedTechSkill(RuntimeAnimatorController controller)
        {
            _controller = controller;
        }

        /// <summary>盤面にぷよが1個以上あれば発動可。</summary>
        public bool CanExecute(GameContext ctx) => ctx.Contents.PuyoBoard.FindDensestRow() >= 0;

        public async UniTask ExecuteAsync(GameContext ctx, CancellationToken ct)
        {
            var board = ctx.Contents.PuyoBoard;

            var handle = await ViewManager.PushViewAsync<SkillCutInView>(
                ViewKeys.SKILL_CUT_IN,
                new SkillCutInView.SkillCutInViewData { Controller = _controller, ImageSize = ImageSize });
            var cutInView = handle?.View as SkillCutInView;
            if (cutInView != null) await cutInView.PlayAttackAsync(ct);
            await ViewManager.PopViewAsync(handle); // アニメ完了後すぐスライドアウト

            // 最多行を左→右にスイープ消去（各セルから矢印＋爆散エフェクト発生）
            var targetRow = board.FindDensestRow();
            var puyoCount = board.GetNonNullCountInRow(targetRow);
            var effect    = GameEffectController.Instance;
            for (var col = 0; col < PuyoBoard.COLS; col++)
            {
                var cell     = new Vector2Int(col, targetRow);
                var worldPos = board.CellToWorld(cell);
                var color    = board.GetColorAt(cell) ?? PuyoColor.RED;

                effect?.PlayArrowSweep(worldPos, ct);
                effect?.PlayBurst(worldPos, color, ct);
                await board.ClearCellBySkillAsync(cell, ct);
                await UniTask.Delay(30, cancellationToken: ct);
            }

            await board.DropFloatingAfterSkillAsync(ct);
            ctx.Enemy?.TakeDamage(DamageCalculator.CalcSkillDamage(puyoCount, 2.0f));
        }
    }
}
