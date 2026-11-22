#nullable enable
using System;
using UnityEngine;

namespace App
{
    /// <summary>
    /// EnemySkillAssets に設定するスキルアイコン1件分のデータ（Type + アイコンSprite）。
    /// </summary>
    [Serializable]
    public struct SkillIconEntry
    {
        public EnemySkillType Type;
        public Sprite?        Icon;
    }
}
#nullable disable
