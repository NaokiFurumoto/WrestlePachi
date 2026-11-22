#nullable enable
using System.Linq;
using System.Threading;
using App.Effects;
using App.Puyo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 岩：一定間隔で上から岩が落ちてきて、盤面のぷよを1つ岩にする（岩の殻がかぶさる）。
    /// 岩になったぷよは元の色のまま連鎖に使えるが、耐久の回数だけ消去に巻き込まれるまで消えない
    /// （1回目でヒビが入り、耐久が尽きると殻が砕けて消える）。レベルが高いほど間隔が短く、耐久値も増える。
    /// 耐久判定・見た目は PuyoBoard.ConvertToRock / RockPuyoState 側にある。
    /// 数値バランスは EnemySkillBalanceConfig.Rock で調整する。
    /// </summary>
    public sealed class RockSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.Rock;

        private readonly EnemySkillBalanceConfig.RockBalance _balance;
        private readonly RockVisuals _visuals;
        private readonly Sprite?     _fallSprite;
        private CancellationTokenSource? _cts;

        public RockSkill(int level, EnemySkillAssets assets, EnemySkillBalanceConfig.RockBalance balance) : base(level)
        {
            _balance    = balance;
            _fallSprite = assets.RockFallSprite;
            _visuals    = new RockVisuals(assets.RockShellSprite, assets.RockShellCrackSprite, assets.RockFallSprite, assets.RockHitSprite);
        }

        public override void OnEnter(GameContext ctx)
        {
            base.OnEnter(ctx);
            _cts = new CancellationTokenSource();
        }

        protected override float GetInterval(int level)
            => Mathf.Max(_balance.MinIntervalSec, _balance.BaseIntervalSec - (level - 1) * _balance.IntervalStepSec);

        protected override int GetPower(int level) => _balance.BaseDurability + (level - 1);

        protected override void Execute(GameContext ctx)
        {
            if (_cts == null) return;
            DropRockAsync(ctx.Contents.PuyoBoard, _cts.Token).Forget();
        }

        /// <summary>まだ岩になっていないぷよを1つ選んで岩を落とし、当たったらそのぷよを岩にする</summary>
        private async UniTaskVoid DropRockAsync(PuyoBoard board, CancellationToken ct)
        {
            var cell = board.GetRandomPuyoPositions(int.MaxValue).FirstOrDefault(board.CanBecomeRock);
            var target = board.CanBecomeRock(cell) ? board.GetPieceAt(cell) : null;
            if (target == null) return;

            if (_fallSprite != null)
            {
                if (await RockFallEffect.DropAsync(board, target, _fallSprite, ct).SuppressCancellationThrow()) return;
                // 落ちている間に連鎖で消えた・落下して別のマスへ移った場合も、そのぷよがまだ盤面にあれば岩にする
                if (target == null || board.GetPieceAt(target.Cell) != target) return;
            }

            board.ConvertToRock(target.Cell, GetPower(Level), _visuals);
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
