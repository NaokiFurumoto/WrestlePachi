#nullable enable
namespace App
{
    /// <summary>
    /// 敵スキルインスタンスの共通マーカー。
    /// EnemySkillBase（周期発動型）・EnemyPassiveSkillBase（常時効果型）の両方が実装する。
    /// EnemySkillFactory の戻り値型として使い、EnemySkillManager 側でどちらの系統かを判定する。
    /// </summary>
    public interface IEnemySkill
    {
        EnemySkillType Type { get; }

        /// <summary>スキルレベル（1始まり）</summary>
        int Level { get; }
    }
}
#nullable disable
