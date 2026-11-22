#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// お邪魔落下（狙い撃ちの拳）：一定間隔で、いちばん高く積まれた列を狙う。
    /// 狙われた列は赤く点滅して予告され、次のペアが出る直前に拳がドスンと叩いて、
    /// その列にお邪魔ぷよがまとめて縦に積まれる。
    /// レベルが高いほど間隔が短く、積まれる数が増える。
    /// 数値バランスは EnemySkillBalanceConfig.GarbageDrop で調整する。
    /// </summary>
    public sealed class GarbageDropSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.GarbageDrop;

        private readonly EnemySkillBalanceConfig.GarbageDropBalance _balance;
        private readonly Sprite? _mark;
        private readonly Sprite? _fist;

        public GarbageDropSkill(int level, EnemySkillAssets assets, EnemySkillBalanceConfig.GarbageDropBalance balance) : base(level)
        {
            _balance = balance;
            _mark    = assets.GetIcon(EnemySkillType.GarbageDrop);
            _fist    = assets.GarbageFistSprite;
        }

        protected override float GetInterval(int level)
            => Mathf.Max(_balance.MinIntervalSec, _balance.BaseIntervalSec - (level - 1) * _balance.IntervalStepSec);

        protected override int GetPower(int level) => _balance.BaseCount + (level - 1);

        protected override void Execute(GameContext ctx)
        {
            var board = ctx.Contents.PuyoBoard;
            var col   = board.PickTallestColumn();
            if (col < 0) return;
            board.AnnounceColumnOjama(col, GetPower(Level), _mark, _fist);
        }
    }
}
#nullable disable
