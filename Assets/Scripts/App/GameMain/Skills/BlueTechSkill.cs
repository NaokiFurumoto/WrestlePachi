using System.Collections.Generic;
using System.Threading;
using App;
using App.Puyo;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;

namespace App.Skills
{
    /// <summary>
    /// 青保留の技：ヒップドロップ。
    /// カットイン後、最も高い縦一列を上から下へ順番に消す。
    /// </summary>
    public sealed class BlueTechSkill : ITechSkill
    {
        private static readonly Vector2 ImageSize = new Vector2(483f, 747f);

        private readonly RuntimeAnimatorController _controller;

        public HoldType TargetType => HoldType.Blue;
        public bool HasCutIn      => true;

        public BlueTechSkill(RuntimeAnimatorController controller)
        {
            _controller = controller;
        }

        /// <summary>盤面にぷよが1個以上あれば発動可。</summary>
        public bool CanExecute(GameContext ctx) => ctx.Contents.PuyoBoard.FindDensestColumn() >= 0;

        public async UniTask ExecuteAsync(GameContext ctx, CancellationToken ct)
        {
            var board = ctx.Contents.PuyoBoard;

            var handle = await ViewManager.PushViewAsync<SkillCutInView>(
                ViewKeys.SKILL_CUT_IN,
                new SkillCutInView.SkillCutInViewData { Controller = _controller, ImageSize = ImageSize });
            var cutInView = handle?.View as SkillCutInView;
            if (cutInView != null) await cutInView.PlayAttackAsync(ct);
            await ViewManager.PopViewAsync(handle); // アニメ完了後すぐスライドアウト

            // 最多列を上→下にスイープ消去（各セルの位置から縦スイープ＋爆散を発生させる）
            var targetCol = board.FindDensestColumn();
            var puyoCount = board.GetNonNullCountInCol(targetCol);
            var topRow    = board.GetColumnTop(targetCol) - 1;
            var effect    = GameEffectController.Instance;

            var clearTasks = new List<UniTask>();
            for (var row = topRow; row >= 0; row--)
            {
                var cell     = new Vector2Int(targetCol, row);
                var worldPos = board.CellToWorld(cell);
                var color    = board.GetColorAt(cell) ?? PuyoColor.BLUE;
                effect?.PlayVerticalSweep(worldPos, ct);
                effect?.PlayBurst(worldPos, color, ct);
                clearTasks.Add(board.ClearCellBySkillAsync(cell, ct));
                await UniTask.Delay(30, cancellationToken: ct);
            }
            await UniTask.WhenAll(clearTasks);

            // 着地時の水飛沫
            var landPos = board.CellToWorld(new Vector2Int(targetCol, 0));
            for (var i = 0; i < 4; i++)
            {
                var splashPos = landPos + new Vector3(Random.Range(-0.5f, 0.5f), 0f, 0f);
                effect?.PlayBurst(splashPos, PuyoColor.BLUE, ct);
            }

            await board.DropFloatingAfterSkillAsync(ct);
            ctx.Enemy?.TakeDamage(DamageCalculator.CalcSkillDamage(puyoCount, 1.8f));
        }
    }
}
