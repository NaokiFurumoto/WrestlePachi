#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// 黒保留強化：ステージ中、黒保留（消化するとお邪魔ぷよが降る特殊保留）の出現率を上げる。
    /// レベルが高いほど分母が小さくなり、出現しやすくなる。
    /// 実際の抽選は GameMainController.Hold.cs の SelectHoldType が
    /// GameContext.BlackHoldDenominatorOverride を参照して行う。
    /// 数値バランスは EnemySkillBalanceConfig.BlackHoldBoost で調整する。
    /// </summary>
    public sealed class BlackHoldBoostSkill : EnemyPassiveSkillBase
    {
        public override EnemySkillType Type => EnemySkillType.BlackHoldBoost;

        private readonly EnemySkillBalanceConfig.BlackHoldBoostBalance _balance;

        public BlackHoldBoostSkill(int level, EnemySkillBalanceConfig.BlackHoldBoostBalance balance) : base(level)
        {
            _balance = balance;
        }

        public override void OnEnter(GameContext ctx)
        {
            ctx.BlackHoldDenominatorOverride = Mathf.Max(
                _balance.MinDenominator, _balance.BaseDenominator - (Level - 1) * _balance.DenominatorStep);
        }

        public override void OnRelease(GameContext ctx)
        {
            ctx.BlackHoldDenominatorOverride = null;
        }
    }
}
#nullable disable
