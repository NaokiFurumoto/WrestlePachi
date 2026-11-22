#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// 岩：一定間隔で盤面上の既存ぷよをランダムに1つ岩化する。
    /// 岩化されたぷよは、通常消去に2回巻き込まれるまで実際には消えない
    /// （1回目のヒットでスプライトが変化する）。レベルが高いほど間隔が短く、耐久値も増える。
    /// 実際の耐久判定・見た目変更処理は PuyoBoard.ConvertToRock / RockPuyoState 側にある。
    /// 数値バランスは EnemySkillBalanceConfig.Rock で調整する。
    /// </summary>
    public sealed class RockSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.Rock;

        private readonly EnemySkillBalanceConfig.RockBalance _balance;
        private readonly Sprite? _hitSprite;

        public RockSkill(int level, EnemySkillAssets assets, EnemySkillBalanceConfig.RockBalance balance) : base(level)
        {
            _hitSprite = assets.RockHitSprite;
            _balance   = balance;
        }

        protected override float GetInterval(int level)
            => Mathf.Max(_balance.MinIntervalSec, _balance.BaseIntervalSec - (level - 1) * _balance.IntervalStepSec);

        protected override int GetPower(int level) => _balance.BaseDurability + (level - 1);

        protected override void Execute(GameContext ctx)
        {
            var board     = ctx.Contents.PuyoBoard;
            var positions = board.GetRandomPuyoPositions(1);
            if (positions.Count == 0) return;

            board.ConvertToRock(positions[0], GetPower(Level), _hitSprite);
        }
    }
}
#nullable disable
