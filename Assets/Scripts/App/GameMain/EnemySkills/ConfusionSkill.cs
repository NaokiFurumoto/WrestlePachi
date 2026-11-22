#nullable enable
using System.Threading;
using App.Effects;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 混乱：一定間隔で発動し、一定時間だけ左右操作を反転させる。
    /// 発動中は目を回したヒヨコがペアの頭上を回り（ConfusionChicksEffect）、1羽ずつ飛び去って残り時間を知らせる。
    /// 操作ボタンの見た目は PuyoInputView が GameMainController.IsInputReversed を見て切り替える。
    /// 数値バランスは EnemySkillBalanceConfig.Confusion で調整する
    /// （レベルごとの発動間隔・持続時間バランスは未確定。仕様が固まり次第調整する）。
    /// </summary>
    public sealed class ConfusionSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.Confusion;

        private readonly EnemySkillBalanceConfig.ConfusionBalance _balance;
        private readonly Sprite? _chickSprite;
        private CancellationTokenSource? _cts;
        private ConfusionChicksEffect? _chicks;

        public ConfusionSkill(int level, EnemySkillAssets assets, EnemySkillBalanceConfig.ConfusionBalance balance) : base(level)
        {
            _balance     = balance;
            _chickSprite = assets.ConfusionChickSprite;
        }

        protected override float GetInterval(int level) => _balance.BaseIntervalSec;

        protected override void Execute(GameContext ctx)
        {
            // 発動中に再度発動した場合は前回の持続時間をキャンセルして上書きする
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            RunReverseAsync(ctx, _cts.Token).Forget();
        }

        private async UniTaskVoid RunReverseAsync(GameContext ctx, CancellationToken ct)
        {
            var duration = _balance.BaseDurationSec + (Level - 1) * _balance.DurationStepSec;
            DismissChicks();
            _chicks = ConfusionChicksEffect.Create(ctx.Contents.PuyoBoard, _chickSprite, duration);
            ctx.IsInputReversed = true;

            await UniTask.Delay((int)(duration * 1000), DelayType.UnscaledDeltaTime, cancellationToken: ct)
                .SuppressCancellationThrow();
            if (ct.IsCancellationRequested) return;

            ctx.IsInputReversed = false;
            DismissChicks();
        }

        /// <summary>前のヒヨコが残っていれば飛び去らせる（時間切れなら自分で消えているので何もしない）</summary>
        private void DismissChicks()
        {
            if (_chicks != null) _chicks.Dismiss();
            _chicks = null;
        }

        public override void OnRelease(GameContext ctx)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            ctx.IsInputReversed = false;
            DismissChicks();
        }
    }
}
#nullable disable
