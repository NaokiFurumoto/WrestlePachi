#nullable enable
using System;
using System.Collections.Generic;

namespace App
{
    /// <summary>
    /// ステージで発動する敵スキルを決めるルール。
    /// 1戦目は得意技そのまま、2戦目（同じ敵との再戦）は「得意技の Lv+1（上限あり）＋ 追加技」。
    /// Unity に依存しない純粋関数にしてあり、EditMode テストで検証する。
    /// </summary>
    public static class EnemySkillRule
    {
        /// <summary>敵スキルLvの上限</summary>
        public const int MaxSkillLevel = 3;

        /// <summary>敵アセットと2戦目フラグから技を決める。敵が null なら技なし。</summary>
        public static EnemySkillEntry[] Resolve(EnemyData? enemy, bool isRematch)
            => enemy == null
                ? Array.Empty<EnemySkillEntry>()
                : Resolve(enemy.SignatureSkills, enemy.RematchExtraSkills, isRematch);

        /// <summary>
        /// 得意技・2戦目の追加技・2戦目フラグから技を決める。
        /// 得意技を先、追加技を後の順に並べ、同じ種類が重複したら先に出た位置に Lv の高い方を残す。
        /// 返り値は常に新しい配列（呼び出し側が書き換えてもアセットには影響しない）。
        /// </summary>
        public static EnemySkillEntry[] Resolve(IReadOnlyList<EnemySkillEntry> signature,
                                                IReadOnlyList<EnemySkillEntry> rematchExtra,
                                                bool isRematch)
        {
            var result = new List<EnemySkillEntry>();

            foreach (var skill in signature)
            {
                var level = isRematch ? Math.Min(skill.Level + 1, MaxSkillLevel) : skill.Level;
                AddOrRaise(result, new EnemySkillEntry { Type = skill.Type, Level = level });
            }

            if (isRematch)
            {
                foreach (var skill in rematchExtra)
                    AddOrRaise(result, skill);
            }

            return result.ToArray();
        }

        /// <summary>同じ種類がなければ追加、あれば Lv の高い方を残す</summary>
        private static void AddOrRaise(List<EnemySkillEntry> list, EnemySkillEntry entry)
        {
            var index = list.FindIndex(e => e.Type == entry.Type);
            if (index < 0)
            {
                list.Add(entry);
                return;
            }
            if (entry.Level > list[index].Level)
                list[index] = entry;
        }
    }
}
#nullable disable
