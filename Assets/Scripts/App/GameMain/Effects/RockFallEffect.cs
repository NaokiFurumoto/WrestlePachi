#nullable enable
using System.Threading;
using App.Puyo;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Effects
{
    /// <summary>
    /// 敵スキル「岩」の落石演出。
    /// 盤面の上から岩が回りながら落ちてきて、狙ったぷよにゴツンと当たると画面が少し揺れ、岩は砕けて散る。
    /// 当たった後に呼び出し側がそのぷよを岩にする（殻がかぶさる）。Prefab は使わず実行時に生成する。
    /// </summary>
    public static class RockFallEffect
    {
        /// <summary>落ちてくる岩の幅（ぷよ何個分か）</summary>
        private const float WidthInCells = 1.4f;
        /// <summary>落ちる時間・当たって砕ける時間（秒）</summary>
        private const float FallTime   = 0.4f;
        private const float ImpactTime = 0.1f;
        /// <summary>盤面のいちばん上から何マス上から落とし始めるか</summary>
        private const float StartAboveInCells = 2f;
        /// <summary>ぷよより何段手前に描くか</summary>
        private const int SortingOrderAbovePuyo = 20;

        /// <summary>岩を target に落とす。当たって砕けたら完了</summary>
        public static async UniTask DropAsync(PuyoBoard board, PuyoPiece target, Sprite sprite, CancellationToken ct)
        {
            var go = new GameObject("RockFallEffect");
            go.transform.SetParent(board.transform, true);
            try
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                board.ApplyPieceSorting(sr, SortingOrderAbovePuyo);

                var cellSize = board.CellSize;
                var scale    = cellSize * WidthInCells / sprite.bounds.size.x;
                go.transform.localScale = new Vector3(scale, scale, 1f);

                var landPos = target.transform.position;
                var topY    = board.CellToWorld(new Vector2Int(target.Cell.x, board.VisibleRows - 1)).y;
                go.transform.position = new Vector3(landPos.x, topY + cellSize * StartAboveInCells, landPos.z);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-30f, 30f));

                // ① 回りながら落ちる（だんだん速く）。落ちている間にぷよが動いたら、着地点もついていく
                var fall = go.transform.DORotate(new Vector3(0f, 0f, Random.Range(-200f, 200f)), FallTime, RotateMode.FastBeyond360).SetLink(go);
                var startPos = go.transform.position;
                DOTween.To(() => 0f, t =>
                {
                    if (target != null) landPos = target.transform.position;
                    go.transform.position = Vector3.LerpUnclamped(startPos, landPos, t);
                }, 1f, FallTime).SetEase(Ease.InQuad).SetLink(go);
                await UniTask.Delay(Mathf.RoundToInt(FallTime * 1000), cancellationToken: ct);
                fall.Kill();

                // ② ゴツン：画面が少し揺れ、岩はつぶれながら砕けて破片が散る
                ScreenShakeController.Shake(ShakePreset.Light);
                RockDebrisEffect.Spawn(landPos, cellSize, sprite, sr, board.transform, 5, 0.9f);
                go.transform.DOScale(new Vector3(scale * 1.3f, scale * 0.6f, 1f), ImpactTime).SetEase(Ease.OutQuad).SetLink(go);
                sr.DOFade(0f, ImpactTime).SetLink(go);
                await UniTask.Delay(Mathf.RoundToInt(ImpactTime * 1000), cancellationToken: ct);
            }
            finally
            {
                if (go != null) Object.Destroy(go);
            }
        }
    }
}
#nullable disable
