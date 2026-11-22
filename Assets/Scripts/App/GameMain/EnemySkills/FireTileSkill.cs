#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 炎：ステージ開始時に盤面上のいくつかのマスを「炎マス」に指定する。
    /// 炎マスにぷよが置かれると、連鎖判定より先に自動で焼き消える（PuyoBoard.BurnFireTilePiecesAsync）。
    /// レベルが高いほど炎マスの数が増える。
    /// 数値バランスは EnemySkillBalanceConfig.FireTile で調整する。
    /// </summary>
    public sealed class FireTileSkill : EnemyPassiveSkillBase
    {
        public override EnemySkillType Type => EnemySkillType.FireTile;

        private readonly EnemySkillBalanceConfig.FireTileBalance _balance;
        private readonly Sprite? _tileSprite;
        private readonly List<Vector2Int> _tiles = new();

        public FireTileSkill(int level, EnemySkillAssets assets, EnemySkillBalanceConfig.FireTileBalance balance) : base(level)
        {
            _tileSprite = assets.FireTileSprite;
            _balance    = balance;
        }

        public override void OnEnter(GameContext ctx)
        {
            var board     = ctx.Contents.PuyoBoard;
            var tileCount = _balance.BaseTileCount + (Level - 1);
            var attempts  = 0;

            while (_tiles.Count < tileCount && attempts < tileCount * 4)
            {
                attempts++;
                var cell = board.GetRandomEmptyCell();
                if (cell == null) break;
                if (board.IsFireTile(cell.Value)) continue;

                board.AddFireTile(cell.Value, _tileSprite);
                _tiles.Add(cell.Value);
            }
        }

        public override void OnRelease(GameContext ctx)
        {
            var board = ctx.Contents.PuyoBoard;
            foreach (var cell in _tiles) board.RemoveFireTile(cell);
            _tiles.Clear();
        }
    }
}
#nullable disable
