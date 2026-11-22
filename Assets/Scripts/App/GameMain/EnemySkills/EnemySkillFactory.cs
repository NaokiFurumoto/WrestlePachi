#nullable enable
using System;

namespace App
{
    /// <summary>
    /// EnemySkillType からスキルインスタンスを生成するファクトリ（GoF Factory Method パターン）。
    /// 新スキル追加時はここに case を足すだけでよい。
    /// </summary>
    public static class EnemySkillFactory
    {
        public static IEnemySkill Create(EnemySkillType type, int level, EnemySkillAssets assets, EnemySkillBalanceConfig balance)
        {
            return type switch
            {
                EnemySkillType.Shark          => new SharkSkill(level, balance.Shark),
                EnemySkillType.FireTile       => new FireTileSkill(level, assets, balance.FireTile),
                EnemySkillType.GarbageDrop    => new GarbageDropSkill(level, balance.GarbageDrop),
                EnemySkillType.Rock           => new RockSkill(level, assets, balance.Rock),
                EnemySkillType.TimeBomb       => new TimeBombSkill(level, assets, balance.TimeBomb),
                EnemySkillType.SkillSeal      => new SkillSealSkill(level, balance.SkillSeal),
                EnemySkillType.Confusion      => new ConfusionSkill(level, balance.Confusion),
                EnemySkillType.BlackHoldBoost => new BlackHoldBoostSkill(level, balance.BlackHoldBoost),
                EnemySkillType.BlackHoldPower => new BlackHoldPowerSkill(level, balance.BlackHoldPower),
                EnemySkillType.Clock          => new ClockSkill(level, balance.Clock),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "未対応の EnemySkillType です"),
            };
        }
    }
}
#nullable disable
