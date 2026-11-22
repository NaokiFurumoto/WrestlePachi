#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// 時限爆弾：一定間隔で盤面の空きマスに爆弾ぷよを設置する。
    /// 放置すると設置から一定秒後に周囲（上下左右+自身の最大5マス）を自動で消去する。
    /// 通常のぷよ消去に隣接して巻き込まれれば、解除されて爆発しない（お邪魔ぷよと同じ扱い）。
    /// レベルが高いほど設置間隔が短くなる。
    /// 実際の設置・爆発処理は PuyoBoard.PlaceBomb / BombPuyoState 側にある。
    /// 数値バランスは EnemySkillBalanceConfig.TimeBomb で調整する。
    /// </summary>
    public sealed class TimeBombSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.TimeBomb;

        private readonly EnemySkillBalanceConfig.TimeBombBalance _balance;
        private readonly Sprite? _bombSprite;

        public TimeBombSkill(int level, EnemySkillAssets assets, EnemySkillBalanceConfig.TimeBombBalance balance) : base(level)
        {
            _bombSprite = assets.BombPuyoSprite;
            _balance    = balance;
        }

        protected override float GetInterval(int level)
            => Mathf.Max(_balance.MinIntervalSec, _balance.BaseIntervalSec - (level - 1) * _balance.IntervalStepSec);

        protected override void Execute(GameContext ctx)
        {
            ctx.Contents.PuyoBoard.PlaceBomb(_bombSprite, _balance.FuseSeconds);
        }
    }
}
#nullable disable
