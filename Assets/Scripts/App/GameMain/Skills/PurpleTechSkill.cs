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

            var ojamaSprite = board.ColorSprites[(int)PuyoColor.OJAMA];
            var effect      = GameEffectController.Instance;

            // ① タックルを全セル並列で再生し、完了まで待つ
            var tackleTasks = new List<UniTask>();
            foreach (var cell in ojamaPositions)
            {
                var worldPos   = board.CellToWorld(cell);
                var tackleFrom = worldPos + Vector3.left * 2.0f;
                if (effect != null)
                    tackleTasks.Add(effect.PlayTackleAsync(tackleFrom, worldPos, ct));
            }
            await UniTask.WhenAll(tackleTasks);

            // ② 全セルを並列で震わせる
            var shakeTasks = new List<UniTask>();
            foreach (var cell in ojamaPositions)
                shakeTasks.Add(board.ShakePuyoAsync(cell, 0.4f, ct));
            await UniTask.WhenAll(shakeTasks);

            // ③ Dissolve を全セル並列で再生し、完了まで待つ
            var dissolveTasks = new List<UniTask>();
            foreach (var cell in ojamaPositions)
            {
                var worldPos = board.CellToWorld(cell);
                if (effect != null)
                    dissolveTasks.Add(effect.PlayOjamaDissolveAsync(worldPos, ojamaSprite, ct));
            }
            await UniTask.WhenAll(dissolveTasks);

            // ④ アニメーションなしで即時消去
            foreach (var cell in ojamaPositions)
                board.RemoveCellInstant(cell);

            await board.DropFloatingAfterSkillAsync(ct);
            ctx.Enemy?.TakeDamage(DamageCalculator.CalcSkillDamage(ojamaPositions.Count, 2.5f));
        }
    }
}
