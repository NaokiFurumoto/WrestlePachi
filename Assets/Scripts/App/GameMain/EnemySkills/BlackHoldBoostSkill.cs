#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// 黒保留強化＝黒塗り：一定間隔で黒い小悪魔が保留スロットへ飛んできて、並んでいる保留のうち
    /// いちばん新しい普通の保留を筆で黒く塗り、黒保留（消化するとお邪魔ぷよが降る）に変えてしまう。
    /// 並んでいなければ次に出てきた保留を塗る。虹・黒・鎖のかかった保留は塗らない（HoldSystem.RequestPaint）。
    /// 塗る演出は HoldDisplay.OnHoldPainted（BlackPaintEffect）。
    /// レベルが高いほど間隔が短い。数値バランスは EnemySkillBalanceConfig.BlackHoldBoost で調整する。
    /// </summary>
    public sealed class BlackHoldBoostSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.BlackHoldBoost;

        private readonly EnemySkillBalanceConfig.BlackHoldBoostBalance _balance;

        public BlackHoldBoostSkill(int level, EnemySkillBalanceConfig.BlackHoldBoostBalance balance) : base(level)
        {
            _balance = balance;
        }

        protected override float GetInterval(int level)
            => Mathf.Max(_balance.MinIntervalSec, _balance.BaseIntervalSec - (level - 1) * _balance.IntervalStepSec);

        protected override void Execute(GameContext ctx) => ctx.RequestHoldPaint?.Invoke();

        public override void OnRelease(GameContext ctx) => ctx.CancelHoldPaint?.Invoke();
    }
}
#nullable disable
