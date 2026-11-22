#nullable enable
using System.Threading;
using App.Puyo;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Effects
{
    /// <summary>
    /// 敵スキル「サメ」の演出。
    /// 狙ったぷよと同じ高さで盤面の左外からサメが泳いできて、口を開けてぷよをパクッと食べ、右へ泳ぎ抜ける。
    /// Prefab は使わず、実行時に SpriteRenderer を生成する。
    /// </summary>
    public static class SharkEffect
    {
        // ─── 見た目の調整値 ──────────────────────────────────────
        /// <summary>サメの長さ（ぷよ何個分か）</summary>
        private const float LengthInCells = 2.5f;
        /// <summary>絵の中で口がある位置（絵の幅・高さに対する中心からの割合。右向きの絵）</summary>
        private static readonly Vector2 MouthOffsetRatio = new(0.42f, -0.02f);
        /// <summary>泳いでくる時間・泳ぎ去る時間・ぷよを吸い込む時間（秒）</summary>
        private const float SwimInTime  = 0.35f;
        private const float SwimOutTime = 0.35f;
        private const float GulpTime    = 0.12f;
        /// <summary>泳いでくる途中、どこから口を開けるか（0〜1）</summary>
        private const float OpenMouthAt = 0.7f;
        /// <summary>ぷよより何段手前に描くか</summary>
        private const int SortingOrderAbovePuyo = 20;

        /// <summary>
        /// サメを1匹泳がせて cell のぷよを食べる。食べられたら true。
        /// ぷよがサメの到着前に消えた・動いた場合は食べずに素通りして false。
        /// </summary>
        public static async UniTask<bool> PlayAsync(
            PuyoBoard board, Vector2Int cell, Sprite openSprite, Sprite closedSprite, CancellationToken ct)
        {
            var target = board.GetPieceAt(cell);
            if (target == null) return false;

            var go       = new GameObject("SharkEffect");
            var detached = false;
            go.transform.SetParent(board.transform, true);
            try
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = closedSprite;
                var targetRenderer = target.GetComponent<SpriteRenderer>();
                sr.sortingLayerID = targetRenderer.sortingLayerID;
                sr.sortingOrder   = targetRenderer.sortingOrder + SortingOrderAbovePuyo;

                // 大きさ：サメの長さをぷよ LengthInCells 個分にする
                var cellSize   = board.CellSize;
                var spriteSize = closedSprite.bounds.size;
                var scale      = cellSize * LengthInCells / spriteSize.x;
                go.transform.localScale = new Vector3(scale, scale, 1f);

                // 位置：口がぷよに重なる地点で食べる。盤面の左外から入り、右外へ抜ける
                var targetPos   = target.transform.position;
                var mouthOffset = new Vector3(spriteSize.x * MouthOffsetRatio.x, spriteSize.y * MouthOffsetRatio.y) * scale;
                var chompPos    = targetPos - mouthOffset;
                var length      = cellSize * LengthInCells;
                var boardLeft   = board.CellToWorld(new Vector2Int(0, cell.y)).x - cellSize * 0.5f;
                var boardRight  = board.CellToWorld(new Vector2Int(PuyoBoard.COLS - 1, cell.y)).x + cellSize * 0.5f;
                var startPos    = new Vector3(boardLeft - length * 0.6f, chompPos.y, targetPos.z);
                var endPos      = new Vector3(boardRight + length * 0.6f, chompPos.y, targetPos.z);
                go.transform.position = startPos;

                // ① 泳いでくる（途中で口を開ける）
                go.transform.DOMove(chompPos, SwimInTime).SetEase(Ease.Linear).SetLink(go);
                await DelaySeconds(SwimInTime * OpenMouthAt, ct);
                sr.sprite = openSprite;
                await DelaySeconds(SwimInTime * (1f - OpenMouthAt), ct);

                // ② パクッ：口を閉じて、ぷよがまだそこにいれば口の中へ吸い込む
                sr.sprite = closedSprite;
                go.transform.DOPunchScale(new Vector3(scale, scale, 0f) * 0.12f, GulpTime * 2f, 6).SetLink(go);
                detached = board.DetachPiece(cell, target);
                if (detached)
                {
                    var mouthPos = chompPos + mouthOffset;
                    target.transform.DOMove(mouthPos, GulpTime).SetEase(Ease.InQuad).SetLink(target.gameObject);
                    target.transform.DOScale(0f, GulpTime).SetEase(Ease.InQuad).SetLink(target.gameObject);
                    await DelaySeconds(GulpTime, ct);
                }

                // ③ 右へ泳ぎ抜ける
                go.transform.DOMove(endPos, SwimOutTime).SetEase(Ease.InQuad).SetLink(go);
                await DelaySeconds(SwimOutTime, ct);
                return detached;
            }
            finally
            {
                // 途中で止まっても（ステージ切替・ゲームオーバー）、サメと食べかけのぷよを残さない
                if (detached && target != null) Object.Destroy(target.gameObject);
                if (go != null) Object.Destroy(go);
            }
        }

        private static UniTask DelaySeconds(float seconds, CancellationToken ct)
            => UniTask.Delay(Mathf.RoundToInt(seconds * 1000), cancellationToken: ct);
    }
}
#nullable disable
