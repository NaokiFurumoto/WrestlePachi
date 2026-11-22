#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;

namespace App
{
    /// <summary>
    /// 混乱：一定間隔で発動し、一定時間だけ左右操作を反転させる。
    /// 数値バランスは EnemySkillBalanceConfig.Confusion で調整する
    /// （レベルごとの発動間隔・持続時間バランスは未確定。仕様が固まり次第調整する）。
    /// </summary>
    public sealed class ConfusionSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.Confusion;

        private readonly EnemySkillBalanceConfig.ConfusionBalance _balance;
        private CancellationTokenSource? _cts;

        public ConfusionSkill(int level, EnemySkillBalanceConfig.ConfusionBalance balance) : base(level)
        {
            _balance = balance;
        }

        protected override float GetInterval(int level) => _balance.BaseIntervalSec;

        protected override void Execute(GameContext ctx)
        {
            // 発動中に再度発動した場合は前回の持続時間をキャンセルして上書きする
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            RunReverseAsync(ctx, _cts.Token).Forget();
        }

        private async UniTaskVoid RunReverseAsync(GameContext ctx, CancellationToken ct)
        {
            ctx.IsInputReversed = true;
            var duration = _balance.BaseDurationSec + (Level - 1) * _balance.DurationStepSec;
            await UniTask.Delay((int)(duration * 1000), DelayType.UnscaledDeltaTime, cancellationToken: ct)
                .SuppressCancellationThrow();
            if (!ct.IsCancellationRequested)
                ctx.IsInputReversed = false;
        }

        public override void OnRelease(GameContext ctx)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            ctx.IsInputReversed = false;
        }
    }
}
#nullable disable
