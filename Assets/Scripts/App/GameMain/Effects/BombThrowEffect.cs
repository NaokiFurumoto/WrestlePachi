#nullable enable
using System.Threading;
using App.Puyo;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Effects
{
    /// <summary>
    /// 敵スキル「時限爆弾」の投げ込み演出。
    /// 盤面の左上（敵のいる側）の外から、爆弾がくるくる回りながら放物線を描いて飛んできて、狙ったマスに着地する。
    /// 着地後に呼び出し側がそのマスへ爆弾ぷよを置く。Prefab は使わず実行時に生成する。
    /// </summary>
    public static class BombThrowEffect
    {
        /// <summary>飛んでいる時間（秒）</summary>
        private const float FlightTime = 0.6f;
        /// <summary>投げ始める位置：盤面の左上の角から何マス外か（左・上）</summary>
        private const float StartLeftInCells = 1.5f;
        private const float StartUpInCells   = 2.5f;
        /// <summary>放物線の高さ（ぷよ何個分か）</summary>
        private const float ArcHeightInCells = 1.5f;
        /// <summary>ぷよより何段手前に描くか</summary>
        private const int SortingOrderAbovePuyo = 20;

        /// <summary>爆弾を cell へ投げ込む。着地したら完了</summary>
        public static async UniTask ThrowAsync(PuyoBoard board, Vector2Int cell, Sprite sprite, CancellationToken ct)
        {
            var go = new GameObject("BombThrowEffect");
            go.transform.SetParent(board.transform, false);
            try
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                board.ApplyPieceSorting(sr, SortingOrderAbovePuyo);
                go.transform.localScale = board.PieceLocalScale;

                var cellSize = board.CellSize;
                var corner   = board.CellToWorld(new Vector2Int(0, board.VisibleRows - 1));
                go.transform.position = corner + new Vector3(-StartLeftInCells, StartUpInCells, 0f) * cellSize;
                var landPos = board.CellToWorld(cell);

                go.transform.DOJump(landPos, ArcHeightInCells * cellSize, 1, FlightTime).SetEase(Ease.Linear).SetLink(go);
                go.transform.DORotate(new Vector3(0f, 0f, -540f), FlightTime, RotateMode.FastBeyond360).SetEase(Ease.OutQuad).SetLink(go);
                await UniTask.Delay(Mathf.RoundToInt(FlightTime * 1000), cancellationToken: ct);
            }
            finally
            {
                if (go != null) Object.Destroy(go);
            }
        }
    }
}
#nullable disable
