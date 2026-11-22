#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace App
{
    /// <summary>
    /// スキル封印：レベルに応じた数の技色を封印する（レインボーは対象外）。
    /// レベル1で1色、レベルが上がるごとに封印数が増え、最大で対象全色（レインボー除く）を封印する。
    /// 封印対象の優先順は EnemySkillBalanceConfig.SkillSeal.SealOrder で調整する。
    /// </summary>
    public sealed class SkillSealSkill : EnemyPassiveSkillBase
    {
        public override EnemySkillType Type => EnemySkillType.SkillSeal;

        private readonly EnemySkillBalanceConfig.SkillSealBalance _balance;
        private readonly List<HoldType> _sealedTypes = new();

        public SkillSealSkill(int level, EnemySkillBalanceConfig.SkillSealBalance balance) : base(level)
        {
            _balance = balance;
        }

        public override void OnEnter(GameContext ctx)
        {
            var order = _balance.SealOrder;
            var count = Mathf.Clamp(Level, 1, order.Length);
            _sealedTypes.AddRange(order.Take(count));
            foreach (var type in _sealedTypes)
                ctx.SealedHoldTypes.Add(type);
        }

        public override void OnRelease(GameContext ctx)
        {
            foreach (var type in _sealedTypes)
                ctx.SealedHoldTypes.Remove(type);
            _sealedTypes.Clear();
        }
    }
}
#nullable disable
