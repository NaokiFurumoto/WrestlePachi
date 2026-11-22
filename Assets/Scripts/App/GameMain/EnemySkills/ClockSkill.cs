#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 時計：一定間隔でいたずらネズミがタイマーをかじり、残り時間を削る。
    /// 1回かじるごとに1秒削るので、かじる回数＝削る秒数（演出は ClockMouseEffect）。
    /// レベルが高いほど間隔が短く、削る秒数も増える。
    /// 実際の減算は GameContext.GnawTime（GameMainController.GnawRemainingTimeAsync）経由で行い、
    /// それが未設定なら GameContext.ReduceTime で一度に削る。
    /// 数値バランスは EnemySkillBalanceConfig.Clock で調整する。
    /// </summary>
    public sealed class ClockSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.Clock;

        private readonly EnemySkillBalanceConfig.ClockBalance _balance;
        private CancellationTokenSource? _cts;

        public ClockSkill(int level, EnemySkillBalanceConfig.ClockBalance balance) : base(level)
        {
            _balance = balance;
        }

        /// <summary>削る秒数からかじる回数を決める（端数は四捨五入、最低1回）</summary>
        public static int BitesFor(float reduceSeconds) => Mathf.Max(1, Mathf.RoundToInt(reduceSeconds));

        public override void OnEnter(GameContext ctx)
        {
            base.OnEnter(ctx);
            _cts = new CancellationTokenSource();
        }

        protected override float GetInterval(int level)
            => Mathf.Max(_balance.MinIntervalSec, _balance.BaseIntervalSec - (level - 1) * _balance.IntervalStepSec);

        protected override void Execute(GameContext ctx)
        {
            var reduceSeconds = _balance.BaseReduceSeconds + (Level - 1) * _balance.ReduceSecondsPerLevel;
            if (ctx.GnawTime == null || _cts == null)
            {
                ctx.ReduceTime?.Invoke(reduceSeconds);
                return;
            }
            ctx.GnawTime(BitesFor(reduceSeconds), _cts.Token).SuppressCancellationThrow().Forget();
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
