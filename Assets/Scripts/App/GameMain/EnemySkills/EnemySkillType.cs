#nullable enable
namespace App
{
    /// <summary>
    /// 敵スキルの種別。EnemyData の SignatureSkills / RematchExtraSkills に設定して敵ごとのギミックを定義する。
    /// </summary>
    public enum EnemySkillType
    {
        Shark,          // サメ        ：周期的にぷよを自動消去してHP回復
        FireTile,       // 炎          ：指定マスを通過したぷよが消滅
        GarbageDrop,    // お邪魔落下  ：周期的にお邪魔ぷよを投下
        Rock,           // 岩        ：2回消さないと消えない特殊ぷよ
        TimeBomb,       // 時限爆弾    ：爆弾ぷよを設置し、放置すると周囲を消去。隣接消去で解除
        SkillSeal,      // スキル封印  ：レベルに応じた数の技色を封印（レインボー除く）
        Confusion,      // 混乱        ：一定時間、左右操作が反転
        BlackHoldBoost, // 黒保留強化  ：黒保留（お邪魔ぷよ）の出現率を上げる
        BlackHoldPower, // 黒保留威力  ：黒保留消化時に降るお邪魔ぷよの数を増やす
        Clock,          // 時計        ：一定間隔で残り時間を削る
    }
}
#nullable disable
