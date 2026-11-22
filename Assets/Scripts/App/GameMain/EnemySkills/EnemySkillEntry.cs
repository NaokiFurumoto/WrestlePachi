#nullable enable
using System;
using UnityEngine;

namespace App
{
    /// <summary>
    /// EnemyData に設定する敵スキル1件分のデータ。
    /// Level が高いほど発動頻度・数・強度などが上がる（解釈は各スキルクラス側に委ねる）。
    /// </summary>
    [Serializable]
    public struct EnemySkillEntry
    {
        public EnemySkillType Type;
        [Min(1)] public int Level;
    }
}
#nullable disable
