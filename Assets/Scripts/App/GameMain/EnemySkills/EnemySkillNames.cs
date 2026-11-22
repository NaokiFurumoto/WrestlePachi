#nullable enable
namespace App
{
    /// <summary>
    /// 敵スキルの表示名。ステージ詳細画面とデバッグウィンドウで共通に使う。
    /// </summary>
    public static class EnemySkillNames
    {
        /// <summary>敵スキルの日本語の表示名を返す</summary>
        public static string Get(EnemySkillType skill) => skill switch
        {
            EnemySkillType.Shark          => "サメ",
            EnemySkillType.FireTile       => "炎",
            EnemySkillType.GarbageDrop    => "お邪魔落下",
            EnemySkillType.Rock           => "岩",
            EnemySkillType.TimeBomb       => "時限爆弾",
            EnemySkillType.SkillSeal      => "スキル封印",
            EnemySkillType.Confusion      => "混乱",
            EnemySkillType.BlackHoldBoost => "黒保留強化",
            EnemySkillType.BlackHoldPower => "黒保留威力",
            EnemySkillType.Clock          => "時計",
            _                             => skill.ToString(),
        };
    }
}
#nullable disable
