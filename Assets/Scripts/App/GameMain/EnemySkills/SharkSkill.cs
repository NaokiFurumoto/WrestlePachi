#nullable enable
using System.Threading;
using App.Effects;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// サメ：一定間隔でサメが盤面を横切ってぷよを食べ、食べた数に応じて敵のHPを回復する。
    /// レベルが高いほど間隔が短く、消去数が増える。
    /// 数値バランスは EnemySkillBalanceConfig.Shark で調整する。
    /// </summary>
    public sealed class SharkSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.Shark;

        private readonly EnemySkillBalanceConfig.SharkBalance _balance;
        private readonly Sprite? _openSprite;
        private readonly Sprite? _closedSprite;
        private CancellationTokenSource? _cts;

        public SharkSkill(int level, EnemySkillAssets assets, EnemySkillBalanceConfig.SharkBalance balance) : base(level)
        {
            _balance      = balance;
            _openSprite   = assets.SharkOpenSprite;
            _closedSprite = assets.SharkClosedSprite;
        }

        public override void OnEnter(GameContext ctx)
        {
            base.OnEnter(ctx);
            _cts = new CancellationTokenSource();
        }

        protected override float GetInterval(int level)
            => Mathf.Max(_balance.MinIntervalSec, _balance.BaseIntervalSec - (level - 1) * _balance.IntervalStepSec);

        protected override int GetPower(int level) => _balance.BaseCount + (level - 1);

        protected override void Execute(GameContext ctx)
        {
            if (_cts == null) return;
            EatAsync(ctx, _cts.Token).Forget();
        }

        /// <summary>
        /// 食べる数だけサメを1匹ずつ順番に泳がせる。狙うぷよは各サメが泳ぎ出す時点で選び直す
        /// （前のサメの間に盤面が変わっていても、そのとき盤面にあるぷよを狙う）。
        /// 1個食べるごとに敵のHPを回復する。
        /// </summary>
        private async UniTaskVoid EatAsync(GameContext ctx, CancellationToken ct)
        {
            var board = ctx.Contents.PuyoBoard;
            for (var i = 0; i < GetPower(Level); i++)
            {
                if (ct.IsCancellationRequested) return;
                var positions = board.GetRandomPuyoPositions(1);
                if (positions.Count == 0) return;

                bool ate;
                if (_openSprite != null && _closedSprite != null)
                {
                    ate = await SharkEffect.PlayAsync(board, positions[0], _openSprite, _closedSprite, ct);
                }
                else
                {
                    // サメの絵が未設定なら、従来どおり吹き飛ばして消し、上のぷよを落とす
                    await board.ClearCellBySkillAsync(positions[0], ct);
                    await board.DropFloatingAfterSkillAsync(ct);
                    ate = true;
                }

                if (ate && !ct.IsCancellationRequested)
                    ctx.Enemy?.Heal(_balance.HealPerPuyo);
            }
        }

        public override void OnRelease(GameContext ctx)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
#nullable disable
