#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;

namespace App.Skills
{
    /// <summary>
    /// 虹保留の技：全消し。
    /// SkillRainbowView（PUSH!演出）→ レインボーカットイン → ClearAllPuyos の順で実行する。
    /// 天撃ボタン（Space キー）は GameMainController.OnInputTengeki 経由で RainbowInputSource を完了させる。
    /// </summary>
    public sealed class RainbowTechSkill : ITechSkill
    {
        private static readonly Vector2 CutInImageSize = new(1082f, 812f);

        private readonly RuntimeAnimatorController? _controller;

        public HoldType TargetType => HoldType.Rainbow;
        public bool HasCutIn      => true;

        public RainbowTechSkill(RuntimeAnimatorController? controller)
        {
            _controller = controller;
        }

        /// <summary>全消しは盤面状態に依存しないため常に発動可能。</summary>
        public bool CanExecute(GameContext ctx) => true;

        public async UniTask ExecuteAsync(GameContext ctx, CancellationToken ct)
        {
            // ── PUSH 待ち ────────────────────────────────────────────────
            var handle = await ViewManager.PushViewAsync<SkillRainbowView>(ViewKeys.SKILL_RAINBOW);
            var view   = handle?.View as SkillRainbowView;

            // View が開き切ってから PUSH 待ち状態をセット
            // （PushViewAsync 中に OnInputTengeki が来ても TCS を early-complete させない）
            var tcs = new UniTaskCompletionSource();
            ctx.RainbowInputSource = tcs;

            try
            {
                if (view != null)
                    await view.WaitForTriggerAsync(tcs, ct);
                else
                    await tcs.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                // バイブ停止・状態クリア（タイムアウトでも PUSH でもここを通る）
                ctx.RainbowVibrationCts?.Cancel();
                ctx.RainbowVibrationCts = null;
                ctx.RainbowInputSource  = null;

                await ViewManager.PopViewAsync(handle);
            }

            // ── レインボーカットイン ──────────────────────────────────────
            var cutInHandle = await ViewManager.PushViewAsync<SkillCutInView>(
                ViewKeys.SKILL_CUT_IN,
                new SkillCutInView.SkillCutInViewData
                {
                    Controller = _controller,
                    ImageSize  = CutInImageSize,
                });
            var cutInView = cutInHandle?.View as SkillCutInView;
            if (cutInView != null) await cutInView.PlayAttackAsync(ct);
            await ViewManager.PopViewAsync(cutInHandle);

            // ── 全消し ────────────────────────────────────────────────────
            ctx.Enemy?.InstantKill();
            await ctx.Contents.PuyoBoard.ClearAllPuyosAsync(ct);
        }
    }
}
