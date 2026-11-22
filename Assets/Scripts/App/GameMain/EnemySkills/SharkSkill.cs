#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// サメ：一定間隔でぷよを自動消去し、消した数に応じて敵のHPを回復する。
    /// レベルが高いほど間隔が短く、消去数が増える。
    /// 数値バランスは EnemySkillBalanceConfig.Shark で調整する。
    /// </summary>
    public sealed class SharkSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.Shark;

        private readonly EnemySkillBalanceConfig.SharkBalance _balance;
        private CancellationTokenSource? _cts;

        public SharkSkill(int level, EnemySkillBalanceConfig.SharkBalance balance) : base(level)
        {
            _balance = balance;
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

        private async UniTaskVoid EatAsync(GameContext ctx, CancellationToken ct)
        {
            var board     = ctx.Contents.PuyoBoard;
            var positions = board.GetRandomPuyoPositions(GetPower(Level));
            foreach (var cell in positions)
            {
                if (ct.IsCancellationRequested) return;
                await board.ClearCellBySkillAsync(cell, ct);
            }
            if (!ct.IsCancellationRequested)
                ctx.Enemy?.Heal(positions.Count * _balance.HealPerPuyo);
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
