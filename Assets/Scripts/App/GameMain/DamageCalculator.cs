#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// ダメージ計算式を一元管理する static クラス。
    /// 連鎖ダメージとスキルダメージの計算をここに集約する。
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// 通常連鎖ダメージを計算する。
        /// 計算式：消えたぷよ数 × 基礎値 × 連鎖倍率
        /// 連鎖倍率：1連鎖=×1.0, 2連鎖=×1.5, 3連鎖=×2.0 … N連鎖=×(0.5N+0.5)
        /// </summary>
        public static int CalcChainDamage(int puyoCount, int chainCount, int baseDamage)
        {
            var multiplier = 1.0f + (chainCount - 1) * 0.5f;
            return Mathf.RoundToInt(puyoCount * baseDamage * multiplier);
        }

        /// <summary>
        /// スキルダメージを計算する。
        /// 計算式：消えたぷよ数 × スキル倍率
        /// </summary>
        public static int CalcSkillDamage(int puyoCount, float multiplier)
            => Mathf.RoundToInt(puyoCount * multiplier);
    }
}
#nullable disable
