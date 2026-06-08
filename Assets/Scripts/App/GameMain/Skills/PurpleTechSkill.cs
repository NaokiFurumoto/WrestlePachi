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
    /// 紫保留の技：タックル。
    /// お邪魔ぷよが存在する場合のみ発動。カットイン後、お邪魔ぷよを一気に全消しする。
    /// お邪魔が0個の場合は CanExecute=false でストックに積まれる。
    /// </summary>
    public sealed class PurpleTechSkill : ITechSkill
    {
        private static readonly Vector2 ImageSize = new Vector2(898f, 443f);

        private readonly RuntimeAnimatorController _controller;

        public HoldType TargetType => HoldType.Purple;
        public bool HasCutIn      => true;

        public PurpleTechSkill(RuntimeAnimatorController controller)
        {
            _controller = controller;
        }

        /// <summary>お邪魔ぷよが1個以上あれば発動可。0個ならストックに積まれる。</summary>
        public bool CanExecute(GameContext ctx) => ctx.Contents.PuyoBoard.GetOjamaPositions().Count > 0;

        public async UniTask ExecuteAsync(GameContext ctx, CancellationToken ct)
        {
            var board = ctx.Contents.PuyoBoard;
            var ojamaPositions = board.GetOjamaPositions();

            var handle = await ViewManager.PushViewAsync<SkillCutInView>(
                ViewKeys.SKILL_CUT_IN,
                new SkillCutInView.SkillCutInViewData { Controller = _controller, ImageSize = ImageSize });
            var cutInView = handle?.View as SkillCutInView;
            if (cutInView != null) await cutInView.PlayAttackAsync(ct);
            await ViewManager.PopViewAsync(handle); // アニメ完了後すぐスライドアウト

            // お邪魔を一気に並列消去（タックルの爆発感）
            var tasks = new List<UniTask>();
            foreach (var cell in ojamaPositions)
                tasks.Add(board.ClearCellBySkillAsync(cell, ct));
            await UniTask.WhenAll(tasks);

            await board.DropFloatingAfterSkillAsync(ct);
            ctx.Enemy?.TakeDamage(DamageCalculator.CalcSkillDamage(ojamaPositions.Count, 2.5f));
        }
    }
}
