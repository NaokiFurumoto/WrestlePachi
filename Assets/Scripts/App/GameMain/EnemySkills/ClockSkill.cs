#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// 時計：一定間隔で残り時間を削る。レベルが高いほど間隔が短く、削る秒数も増える。
    /// 実際の減算は GameContext.ReduceTime（GameMainController.ReduceRemainingTime）経由で行う。
    /// 数値バランスは EnemySkillBalanceConfig.Clock で調整する。
    /// </summary>
    public sealed class ClockSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.Clock;

        private readonly EnemySkillBalanceConfig.ClockBalance _balance;

        public ClockSkill(int level, EnemySkillBalanceConfig.ClockBalance balance) : base(level)
        {
            _balance = balance;
        }

        protected override float GetInterval(int level)
            => Mathf.Max(_balance.MinIntervalSec, _balance.BaseIntervalSec - (level - 1) * _balance.IntervalStepSec);

        protected override void Execute(GameContext ctx)
        {
            var reduceSeconds = _balance.BaseReduceSeconds + (Level - 1) * _balance.ReduceSecondsPerLevel;
            ctx.ReduceTime?.Invoke(reduceSeconds);
        }
    }
}
#nullable disable
