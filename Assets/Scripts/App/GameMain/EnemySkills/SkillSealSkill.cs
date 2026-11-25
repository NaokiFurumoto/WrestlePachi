#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// スキル封印：一定間隔で敵が狙い（共通の予告が出る）、次に出てきた保留に鎖をかけてロックする。
    /// ロックされた保留は消化のとき砕けて不発になる（HoldSystem）。虹保留と黒保留はロックしない。
    /// レベルが高いほど間隔が短い。数値バランスは EnemySkillBalanceConfig.SkillSeal で調整する。
    /// </summary>
    public sealed class SkillSealSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.SkillSeal;

        private readonly EnemySkillBalanceConfig.SkillSealBalance _balance;

        public SkillSealSkill(int level, EnemySkillBalanceConfig.SkillSealBalance balance) : base(level)
        {
            _balance = balance;
        }

        protected override float GetInterval(int level)
            => Mathf.Max(_balance.MinIntervalSec, _balance.BaseIntervalSec - (level - 1) * _balance.IntervalStepSec);

        protected override void Execute(GameContext ctx) => ctx.RequestHoldLock?.Invoke();

        public override void OnRelease(GameContext ctx) => ctx.CancelHoldLock?.Invoke();
    }
}
#nullable disable
