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
    /// 炎：列のいちばん上（次にぷよが乗る場所）へ火の玉が落ちてきて、炎が燃え続ける。
    /// その列にぷよが落ちて炎にぶつかると、そのぷよを1つ焼いて炎は燃え尽きる（PuyoBoard.BurnFireTilePiecesAsync）。
    /// 炎は盤面が動くとその列の新しい「いちばん上」へついて回る（PuyoBoard.RelocateFireTilesToSurface）。
    /// ステージ開始時に上限まで落とし、その後は一定間隔で、燃え尽きた分だけ補充する。
    /// レベルが高いほど同時に燃える炎が増え、補充間隔が短くなる。
    /// 数値バランスは EnemySkillBalanceConfig.FireTile で調整する。
    /// </summary>
    public sealed class FireTileSkill : EnemySkillBase
    {
        public override EnemySkillType Type => EnemySkillType.FireTile;

        /// <summary>複数の火の玉を落とすときの間隔（秒）</summary>
        private const float DropStagger = 0.15f;

        private readonly EnemySkillBalanceConfig.FireTileBalance _balance;
        private readonly Sprite?   _tileSprite;
        private readonly Sprite?   _ballSprite;
        private readonly Material? _tileMaterial;

        /// <summary>このスキルが燃やした列（燃え尽きたかは盤面に問い合わせる）</summary>
        private readonly List<int> _columns = new();
        /// <summary>火の玉が落下中の列（まだ燃えていないが、二重に狙わないよう数に含める）</summary>
        private readonly HashSet<int> _falling = new();
        private CancellationTokenSource? _cts;

        public FireTileSkill(int level, EnemySkillAssets assets, EnemySkillBalanceConfig.FireTileBalance balance) : base(level)
        {
            _tileSprite   = assets.FireTileSprite;
            _ballSprite   = assets.FireBallSprite;
            _tileMaterial = assets.FireTileMaterial;
            _balance      = balance;
        }

        /// <summary>同時に燃える炎の上限</summary>
        private int MaxFlames => _balance.BaseTileCount + (Level - 1);

        protected override float GetInterval(int level)
            => Mathf.Max(_balance.MinIntervalSec, _balance.BaseIntervalSec - (level - 1) * _balance.IntervalStepSec);

        public override void OnEnter(GameContext ctx)
        {
            base.OnEnter(ctx);
            _cts = new CancellationTokenSource();
            // ステージ開始時（技が付いた時）にまず上限まで落とす
            ctx.ShowEnemySkillWarning?.Invoke(Type);
            Refill(ctx);
        }

        /// <summary>一定間隔ごと：燃え尽きた分だけ火の玉を補充する</summary>
        protected override void Execute(GameContext ctx) => Refill(ctx);

        private void Refill(GameContext ctx)
        {
            if (_cts == null) return;
            var board = ctx.Contents.PuyoBoard;

            // 燃え尽きた列は記録から外す
            _columns.RemoveAll(col => !board.IsColumnOnFire(col));

            var shortage = MaxFlames - _columns.Count - _falling.Count;
            if (shortage <= 0) return;

            // 燃やす列を選ぶ：まだ燃えていない・落下中でない・満杯でない列からランダムに
            var columns = Enumerable.Range(0, PuyoBoard.COLS)
                .Where(col => !board.IsColumnOnFire(col) && !_falling.Contains(col) && board.GetColumnSurface(col) != null)
                .OrderBy(_ => Random.value)
                .Take(shortage)
                .ToList();

            foreach (var col in columns) _falling.Add(col);
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
            try
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
            finally
            {
                _falling.Remove(column);
            }
        }

        public override void OnRelease(GameContext ctx)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            var board = ctx.Contents.PuyoBoard;
            foreach (var column in _columns) board.RemoveFireTileInColumn(column);
            _columns.Clear();
            _falling.Clear();
        }
    }
}
#nullable disable
