#nullable enable
using DG.Tweening;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>
    /// 敵スキル「お邪魔落下」の予告表示。狙われた列が縦の帯で赤く点滅し、
    /// 列の上（盤面の外）にマークが出てふわふわ上下する。Prefab は使わず実行時に作る。
    /// </summary>
    public static class ColumnTargetMarker
    {
        /// <summary>帯の色と、点滅の濃さ（薄い〜濃い）・速さ（秒）</summary>
        private static readonly Color BandColor = new(1f, 0.15f, 0.12f, 1f);
        private const float BandAlphaMin = 0.10f;
        private const float BandAlphaMax = 0.32f;
        private const float BlinkTime    = 0.35f;
        /// <summary>帯をぷよより何段奥に描くか（盤面背景より手前）・マークを何段手前に描くか</summary>
        private const int BandOrderBelowPuyo = -15;
        private const int MarkOrderAbovePuyo = 22;
        /// <summary>マークの大きさ（ぷよ何個分か）</summary>
        private const float MarkSizeInCells = 0.95f;

        /// <summary>列 col に予告表示を出す</summary>
        public static GameObject Create(PuyoBoard board, int col, Sprite? mark)
        {
            var cellSize = board.CellSize;
            var root = new GameObject($"ColumnTarget_{col}");
            root.transform.SetParent(board.transform, false);

            // 縦の帯（列の表示範囲いっぱい）
            var bottom = board.CellToWorld(new Vector2Int(col, 0));
            var top    = board.CellToWorld(new Vector2Int(col, board.VisibleRows - 1));
            var band   = new GameObject("Band");
            band.transform.SetParent(root.transform, true);
            band.transform.position   = (bottom + top) * 0.5f;
            band.transform.localScale = new Vector3(cellSize * 0.96f, cellSize * board.VisibleRows, 1f);
            var bandSr = band.AddComponent<SpriteRenderer>();
            bandSr.sprite = FallbackSkillVisuals.SquareSprite;
            bandSr.color  = new Color(BandColor.r, BandColor.g, BandColor.b, BandAlphaMin);
            board.ApplyPieceSorting(bandSr, BandOrderBelowPuyo);
            bandSr.DOFade(BandAlphaMax, BlinkTime).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(band);

            // 列の上のマーク（ポンと出て、ふわふわ上下する）
            if (mark != null)
            {
                var markGo = new GameObject("Mark");
                markGo.transform.SetParent(root.transform, true);
                markGo.transform.position = top + Vector3.up * (cellSize * 1.05f);
                var markSr = markGo.AddComponent<SpriteRenderer>();
                markSr.sprite = mark;
                board.ApplyPieceSorting(markSr, MarkOrderAbovePuyo);
                var scale = cellSize * MarkSizeInCells / mark.bounds.size.x;
                markGo.transform.localScale = Vector3.zero;
                DOTween.Sequence()
                    .Append(markGo.transform.DOScale(scale, 0.2f).SetEase(Ease.OutBack))
                    .Append(markGo.transform.DOLocalMoveY(markGo.transform.localPosition.y - cellSize * 0.15f, 0.4f)
                        .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo))
                    .SetLink(markGo);
            }
            return root;
        }

        /// <summary>予告表示を消す（さっと薄くなって消える）</summary>
        public static void Dismiss(GameObject marker)
        {
            foreach (var sr in marker.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.DOKill();
                sr.transform.DOKill();
                sr.DOFade(0f, 0.15f).SetLink(sr.gameObject);
            }
            Object.Destroy(marker, 0.2f);
        }
    }
}
#nullable disable
