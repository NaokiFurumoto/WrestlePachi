#nullable enable
using System.Collections.Generic;
using System.Threading;
using App.Effects;
using App.Puyo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 炎：ステージ開始時に火の玉が落ちてきて、着弾したマスが「炎マス」になる。
    /// 炎マスにぷよが置かれると、連鎖判定より先に自動で焼き消える（PuyoBoard.BurnFireTilePiecesAsync）。
    /// レベルが高いほど炎マスの数が増える。
    /// 数値バランスは EnemySkillBalanceConfig.FireTile で調整する。
    /// </summary>
    public sealed class FireTileSkill : EnemyPassiveSkillBase
    {
        public override EnemySkillType Type => EnemySkillType.FireTile;

        /// <summary>複数の火の玉を落とすときの間隔（秒）</summary>
        private const float DropStagger = 0.15f;

        private readonly EnemySkillBalanceConfig.FireTileBalance _balance;
        private readonly Sprite? _tileSprite;
        private readonly Sprite? _ballSprite;
        private readonly List<Vector2Int> _tiles = new();
        private CancellationTokenSource? _cts;

        public FireTileSkill(int level, EnemySkillAssets assets, EnemySkillBalanceConfig.FireTileBalance balance) : base(level)
        {
            _tileSprite = assets.FireTileSprite;
            _ballSprite = assets.FireBallSprite;
            _balance    = balance;
        }

        public override void OnEnter(GameContext ctx)
        {
            var board     = ctx.Contents.PuyoBoard;
            var tileCount = _balance.BaseTileCount + (Level - 1);
            var attempts  = 0;

            // 炎マスにするマスを先に決める（同じマスを2回選ばない）
            var cells = new List<Vector2Int>();
            while (cells.Count < tileCount && attempts < tileCount * 4)
            {
                attempts++;
                var cell = board.GetRandomEmptyCell();
                if (cell == null) break;
                if (board.IsFireTile(cell.Value) || cells.Contains(cell.Value)) continue;
                cells.Add(cell.Value);
            }

            _cts = new CancellationTokenSource();
            DropFireBallsAsync(board, cells, _cts.Token).Forget();
        }

        /// <summary>
        /// 火の玉を少しずつ時間をずらして落とし、着弾したマスから炎マスにする。
        /// 火の玉の絵が未設定なら、落とさずにすぐ炎マスにする。
        /// </summary>
        private async UniTaskVoid DropFireBallsAsync(PuyoBoard board, List<Vector2Int> cells, CancellationToken ct)
        {
            for (var i = 0; i < cells.Count; i++)
            {
                if (i > 0 && await UniTask.Delay(Mathf.RoundToInt(DropStagger * 1000), cancellationToken: ct)
                        .SuppressCancellationThrow()) return;

                FallAndIgniteAsync(board, cells[i], ct).Forget();
            }
        }

        private async UniTaskVoid FallAndIgniteAsync(PuyoBoard board, Vector2Int cell, CancellationToken ct)
        {
            if (_ballSprite != null)
            {
                if (await FireBallEffect.DropAsync(board, cell, _ballSprite, ct).SuppressCancellationThrow()) return;
            }
            if (ct.IsCancellationRequested) return;

            board.AddFireTile(cell, _tileSprite);
            _tiles.Add(cell);
        }

        public override void OnRelease(GameContext ctx)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            var board = ctx.Contents.PuyoBoard;
            foreach (var cell in _tiles) board.RemoveFireTile(cell);
            _tiles.Clear();
        }
    }
}
#nullable disable
