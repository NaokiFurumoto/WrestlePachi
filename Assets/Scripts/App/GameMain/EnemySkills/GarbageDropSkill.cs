#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// お邪魔落下：一定間隔でお邪魔ぷよを盤面に降らせる。
    /// レベルが高いほど間隔が短く、落下数が増える。
    /// 数値バランスは EnemySkillBalanceConfig.GarbageDrop で調整する。
    /// </summary>
    public sealed class GarbageDropSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.GarbageDrop;

        private readonly EnemySkillBalanceConfig.GarbageDropBalance _balance;

        public GarbageDropSkill(int level, EnemySkillBalanceConfig.GarbageDropBalance balance) : base(level)
        {
            _balance = balance;
        }

        protected override float GetInterval(int level)
            => Mathf.Max(_balance.MinIntervalSec, _balance.BaseIntervalSec - (level - 1) * _balance.IntervalStepSec);

        protected override int GetPower(int level) => _balance.BaseCount + (level - 1);

        protected override void Execute(GameContext ctx)
        {
            ctx.Contents.PuyoBoard.AddPendingOjama(GetPower(Level));
        }
    }
}
#nullable disable
