#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using App.Effects;
using App.Puyo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 炎：ステージ開始時に、いくつかの列のいちばん上（次にぷよが乗る場所）へ火の玉が落ちてきて燃え続ける。
    /// その列に落としたぷよは、最初の1個が連鎖判定より先に焼き消える（PuyoBoard.BurnFireTilePiecesAsync）。
    /// 盤面が動くと、炎はその列の新しい「いちばん上」へついて回る（PuyoBoard.RelocateFireTilesToSurface）。
    /// レベルが高いほど燃える列が増える。
    /// 数値バランスは EnemySkillBalanceConfig.FireTile で調整する。
    /// </summary>
    public sealed class FireTileSkill : EnemyPassiveSkillBase
    {
        public override EnemySkillType Type => EnemySkillType.FireTile;

        /// <summary>複数の火の玉を落とすときの間隔（秒）</summary>
        private const float DropStagger = 0.15f;

        private readonly EnemySkillBalanceConfig.FireTileBalance _balance;
        private readonly Sprite?   _tileSprite;
        private readonly Sprite?   _ballSprite;
        private readonly Material? _tileMaterial;
        /// <summary>このスキルが燃やしている列（解除時にこの列の炎だけを消す）</summary>
        private readonly List<int> _columns = new();
        private CancellationTokenSource? _cts;

        public FireTileSkill(int level, EnemySkillAssets assets, EnemySkillBalanceConfig.FireTileBalance balance) : base(level)
        {
            _tileSprite   = assets.FireTileSprite;
            _ballSprite   = assets.FireBallSprite;
            _tileMaterial = assets.FireTileMaterial;
            _balance      = balance;
        }

        public override void OnEnter(GameContext ctx)
        {
            var board     = ctx.Contents.PuyoBoard;
            var tileCount = _balance.BaseTileCount + (Level - 1);

            // 燃やす列を選ぶ：まだ燃えていない・満杯でない列からランダムに（同じ列は選ばない）
            var columns = Enumerable.Range(0, PuyoBoard.COLS)
                .Where(col => !board.IsColumnOnFire(col) && board.GetColumnSurface(col) != null)
                .OrderBy(_ => Random.value)
                .Take(tileCount)
                .ToList();

            _cts = new CancellationTokenSource();
            DropFireBallsAsync(board, columns, _cts.Token).Forget();
        }

        /// <summary>
        /// 火の玉を少しずつ時間をずらして落とし、着弾した列から燃やす。
        /// 火の玉の絵が未設定なら、落とさずにすぐ燃やす。
        /// </summary>
        private async UniTaskVoid DropFireBallsAsync(PuyoBoard board, List<int> columns, CancellationToken ct)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                if (i > 0 && await UniTask.Delay(Mathf.RoundToInt(DropStagger * 1000), cancellationToken: ct)
                        .SuppressCancellationThrow()) return;

                FallAndIgniteAsync(board, columns[i], ct).Forget();
            }
        }

        private async UniTaskVoid FallAndIgniteAsync(PuyoBoard board, int column, CancellationToken ct)
        {
            var target = board.GetColumnSurface(column);
            if (target == null) return;

            if (_ballSprite != null)
            {
                if (await FireBallEffect.DropAsync(board, target.Value, _ballSprite, ct).SuppressCancellationThrow()) return;
            }
            if (ct.IsCancellationRequested) return;

            // 落ちている間に盤面が変わっていても、着弾した時点のいちばん上で燃やす
            var cell = board.GetColumnSurface(column);
            if (cell == null || board.IsColumnOnFire(column)) return;
            board.AddFireTile(cell.Value, _tileSprite, _tileMaterial);
            _columns.Add(column);
        }

        public override void OnRelease(GameContext ctx)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            var board = ctx.Contents.PuyoBoard;
            foreach (var column in _columns) board.RemoveFireTileInColumn(column);
            _columns.Clear();
        }
    }
}
#nullable disable
