#nullable enable
namespace App
{
    /// <summary>
    /// 黒保留威力：ステージ中、黒保留（消化するとお邪魔ぷよが降る特殊保留）1回あたりの
    /// お邪魔ぷよ落下数を増やす。レベルが高いほど落下数が増える。
    /// 実際の適用は Skills/BlackTechSkill.ExecuteAsync が
    /// GameContext.BlackHoldOjamaCountOverride を参照して行う。
    /// 数値バランスは EnemySkillBalanceConfig.BlackHoldPower で調整する。
    /// </summary>
    public sealed class BlackHoldPowerSkill : EnemyPassiveSkillBase
    {
        public override EnemySkillType Type => EnemySkillType.BlackHoldPower;

        private readonly EnemySkillBalanceConfig.BlackHoldPowerBalance _balance;

        public BlackHoldPowerSkill(int level, EnemySkillBalanceConfig.BlackHoldPowerBalance balance) : base(level)
        {
            _balance = balance;
        }

        public override void OnEnter(GameContext ctx)
        {
            ctx.BlackHoldOjamaCountOverride = _balance.BaseOjamaCount + (Level - 1) * _balance.OjamaCountStep;
        }

        public override void OnRelease(GameContext ctx)
        {
            ctx.BlackHoldOjamaCountOverride = null;
        }
    }
}
#nullable disable
