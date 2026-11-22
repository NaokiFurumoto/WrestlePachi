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
    /// 時限爆弾：一定間隔で、敵が爆弾を盤面に投げ込む（ランダムな列のいちばん上に着地する）。
    /// 放置すると設置から一定秒後に周囲（上下左右+自身の最大5マス）を吹き飛ばす。
    /// 通常のぷよ消去に隣接して巻き込まれれば、解除されて爆発しない（お邪魔ぷよと同じ扱い）。
    /// レベルが高いほど設置間隔が短くなる。
    /// 設置・爆発・見た目は PuyoBoard.PlaceBombAt / BombPuyoState 側にある。
    /// 数値バランスは EnemySkillBalanceConfig.TimeBomb で調整する。
    /// </summary>
    public sealed class TimeBombSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.TimeBomb;

        private readonly EnemySkillBalanceConfig.TimeBombBalance _balance;
        private readonly BombVisuals _visuals;
        private CancellationTokenSource? _cts;

        public TimeBombSkill(int level, EnemySkillAssets assets, EnemySkillBalanceConfig.TimeBombBalance balance) : base(level)
        {
            _balance = balance;
            _visuals = new BombVisuals(assets.BombPuyoSprite, assets.BombExplosionSprite, assets.BombFuseTip);
        }

        public override void OnEnter(GameContext ctx)
        {
            base.OnEnter(ctx);
            _cts = new CancellationTokenSource();
        }

        protected override float GetInterval(int level)
            => Mathf.Max(_balance.MinIntervalSec, _balance.BaseIntervalSec - (level - 1) * _balance.IntervalStepSec);

        protected override void Execute(GameContext ctx)
        {
            if (_cts == null) return;
            ThrowBombAsync(ctx.Contents.PuyoBoard, _cts.Token).Forget();
        }

        /// <summary>置ける列をランダムに選んで爆弾を投げ込み、着地したらその列のいちばん上に置く</summary>
        private async UniTaskVoid ThrowBombAsync(PuyoBoard board, CancellationToken ct)
        {
            var columns = Enumerable.Range(0, PuyoBoard.COLS).Where(c => board.GetColumnSurface(c) != null).ToList();
            if (columns.Count == 0) return;
            var col  = columns[Random.Range(0, columns.Count)];
            var cell = board.GetColumnSurface(col)!.Value;

            if (_visuals.Bomb != null)
            {
                if (await BombThrowEffect.ThrowAsync(board, cell, _visuals.Bomb, ct).SuppressCancellationThrow()) return;
                // 飛んでいる間に列の高さが変わっていたら、今のいちばん上に置く（埋まっていたらあきらめる）
                var surface = board.GetColumnSurface(col);
                if (surface == null) return;
                cell = surface.Value;
            }

            board.PlaceBombAt(cell, _balance.FuseSeconds, _visuals);
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
