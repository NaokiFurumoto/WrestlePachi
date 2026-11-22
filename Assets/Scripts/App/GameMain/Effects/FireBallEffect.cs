#nullable enable
using System.Threading;
using App.Puyo;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Effects
{
    /// <summary>
    /// 敵スキル「炎」の火の玉演出。
    /// 盤面の上から火の玉が落ちてきて、狙ったマスに着弾するとボッと膨らんで消える。
    /// 着弾後に呼び出し側がそのマスを炎マスにする。Prefab は使わず実行時に生成する。
    /// </summary>
    public static class FireBallEffect
    {
        /// <summary>火の玉の幅（ぷよ何個分か）</summary>
        private const float WidthInCells = 0.9f;
        /// <summary>落ちる時間・着弾でボッと膨らんで消える時間（秒）</summary>
        private const float FallTime   = 0.35f;
        private const float ImpactTime = 0.12f;
        /// <summary>盤面のいちばん上から何マス上から落とし始めるか</summary>
        private const float StartAboveInCells = 2f;
        /// <summary>ぷよより何段手前に描くか</summary>
        private const int SortingOrderAbovePuyo = 20;

        /// <summary>火の玉を cell に落とす。着弾して消えたら完了</summary>
        public static async UniTask DropAsync(PuyoBoard board, Vector2Int cell, Sprite sprite, CancellationToken ct)
        {
            var go = new GameObject("FireBallEffect");
            go.transform.SetParent(board.transform, true);
            try
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                board.ApplyPieceSorting(sr, SortingOrderAbovePuyo);

                var cellSize = board.CellSize;
                var scale    = cellSize * WidthInCells / sprite.bounds.size.x;
                go.transform.localScale = new Vector3(scale, scale, 1f);

                // 炎の尾が上なので、絵の下半分（丸い部分）がマスに当たるよう少し上にずらして着地させる
                var landPos  = board.CellToWorld(cell) + new Vector3(0f, sprite.bounds.size.y * scale * 0.2f, 0f);
                var topY     = board.CellToWorld(new Vector2Int(cell.x, board.VisibleRows - 1)).y;
                go.transform.position = new Vector3(landPos.x, topY + cellSize * StartAboveInCells, landPos.z);

                // ① 落ちる（だんだん速く）
                go.transform.DOMove(landPos, FallTime).SetEase(Ease.InQuad).SetLink(go);
                await DelaySeconds(FallTime, ct);

                // ② 着弾：ボッと膨らみながら消える
                go.transform.DOScale(new Vector3(scale * 1.6f, scale * 1.2f, 1f), ImpactTime).SetEase(Ease.OutQuad).SetLink(go);
                sr.DOFade(0f, ImpactTime).SetLink(go);
                await DelaySeconds(ImpactTime, ct);
            }
            finally
            {
                if (go != null) Object.Destroy(go);
            }
        }

        private static UniTask DelaySeconds(float seconds, CancellationToken ct)
            => UniTask.Delay(Mathf.RoundToInt(seconds * 1000), cancellationToken: ct);
    }
}
#nullable disable
