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

        // ─── 泳いでいる感（体の揺れ・泡） ───────────────────────────
        /// <summary>体の上下の揺れ幅（ぷよ何個分か）と、1往復の時間（秒）</summary>
        private const float BobAmplitudeInCells = 0.06f;
        private const float BobPeriod           = 0.25f;
        /// <summary>首振りの角度（度）</summary>
        private const float WiggleDegrees = 4f;
        /// <summary>尾びれの位置（絵の幅に対する中心からの割合。右向きの絵なので左側）</summary>
        private const float TailOffsetRatio = -0.46f;
        /// <summary>泡を出す間隔（秒）・泡が消えるまでの時間（秒）・泡の大きさ（ぷよ何個分か）</summary>
        private const float BubbleInterval = 0.033f;
        private const float BubbleLife     = 0.35f;
        private const float BubbleSizeMin  = 0.22f;
        private const float BubbleSizeMax  = 0.45f;

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
                // 絵は子オブジェクト（Body）に付け、親の移動とは別に体を揺らせるようにする
                var body = new GameObject("Body").transform;
                body.SetParent(go.transform, false);
                var sr = body.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = closedSprite;
                var targetRenderer = target.GetComponent<SpriteRenderer>();
                sr.sortingLayerID = targetRenderer.sortingLayerID;
                sr.sortingOrder   = targetRenderer.sortingOrder + SortingOrderAbovePuyo;

                // 大きさ：サメの長さをぷよ LengthInCells 個分にする
                var cellSize   = board.CellSize;
                var spriteSize = closedSprite.bounds.size;
                var scale      = cellSize * LengthInCells / spriteSize.x;
                go.transform.localScale = new Vector3(scale, scale, 1f);

                // 泳いでいる感：体を上下にゆらゆら＋少し首を振る（ずっと繰り返す）
                var bobLocal = cellSize * BobAmplitudeInCells / scale;
                body.DOLocalMoveY(bobLocal, BobPeriod * 0.5f).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetLink(go);
                body.localRotation = Quaternion.Euler(0f, 0f, -WiggleDegrees);
                body.DOLocalRotate(new Vector3(0f, 0f, WiggleDegrees), BobPeriod * 0.5f).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetLink(go);

                // 尾びれの後ろから出す泡の位置（サメの中心からの距離）
                var tailOffset   = new Vector3(spriteSize.x * TailOffsetRatio, 0f) * scale;
                var bubbleLayer  = sr.sortingLayerID;
                var bubbleOrder  = sr.sortingOrder - 1;

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

                // ① 泳いでくる（途中で口を開ける）。尾びれから泡を出し続ける
                using (var bubbles = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    SpawnBubblesAsync(go.transform, tailOffset, cellSize, bubbleLayer, bubbleOrder, bubbles.Token).Forget();
                    go.transform.DOMove(chompPos, SwimInTime).SetEase(Ease.Linear).SetLink(go);
                    await DelaySeconds(SwimInTime * OpenMouthAt, ct);
                    sr.sprite = openSprite;
                    await DelaySeconds(SwimInTime * (1f - OpenMouthAt), ct);
                    bubbles.Cancel(); // パクッの一瞬は泡を止める
                }

                // ② パクッ：口を閉じて、ぷよがまだそこにいれば口の中へ吸い込む
                sr.sprite = closedSprite;
                go.transform.DOPunchScale(new Vector3(scale, scale, 0f) * 0.12f, GulpTime * 2f, 6).SetLink(go);
                detached = board.DetachPiece(cell, target);
                if (detached)
                {
                    // 食べたぷよの上に乗っていたぷよを落とす（サメが泳ぎ去るのは待たない）
                    board.DropFloatingAfterSkillAsync(ct).Forget();
                    var mouthPos = chompPos + mouthOffset;
                    target.transform.DOMove(mouthPos, GulpTime).SetEase(Ease.InQuad).SetLink(target.gameObject);
                    target.transform.DOScale(0f, GulpTime).SetEase(Ease.InQuad).SetLink(target.gameObject);
                    await DelaySeconds(GulpTime, ct);
                }

                // ③ 右へ泳ぎ抜ける（また泡を出す）
                using (var bubbles = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    SpawnBubblesAsync(go.transform, tailOffset, cellSize, bubbleLayer, bubbleOrder, bubbles.Token).Forget();
                    go.transform.DOMove(endPos, SwimOutTime).SetEase(Ease.InQuad).SetLink(go);
                    await DelaySeconds(SwimOutTime, ct);
                    bubbles.Cancel();
                }
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

        // ─── 泡のジェット ─────────────────────────────────────────

        /// <summary>
        /// 止められるまで、サメの尾びれの後ろから白い泡を一定間隔で出し続ける。
        /// 泡は泳ぐ向きと逆（左）へ流れ、ふわっと浮きながら縮んで消える。
        /// </summary>
        private static async UniTaskVoid SpawnBubblesAsync(
            Transform shark, Vector3 tailOffset, float cellSize, int sortingLayerId, int sortingOrder,
            CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && shark != null)
            {
                SpawnBubble(shark.position + tailOffset, shark.parent, cellSize, sortingLayerId, sortingOrder);
                if (await UniTask.Delay(Mathf.RoundToInt(BubbleInterval * 1000), cancellationToken: ct)
                        .SuppressCancellationThrow()) return;
            }
        }

        /// <summary>泡を1つ出す。動き終わったら自分で消える</summary>
        private static void SpawnBubble(Vector3 tailPos, Transform? parent, float cellSize, int sortingLayerId, int sortingOrder)
        {
            var bubble = new GameObject("SharkBubble");
            bubble.transform.SetParent(parent, true);
            bubble.transform.position = tailPos + new Vector3(0f, Random.Range(-0.15f, 0.15f) * cellSize, 0f);
            var size = Random.Range(BubbleSizeMin, BubbleSizeMax) * cellSize;
            bubble.transform.localScale = Vector3.one * size;

            var sr = bubble.AddComponent<SpriteRenderer>();
            sr.sprite         = FallbackSkillVisuals.SoftCircleSprite;
            sr.color          = new Color(1f, 1f, 1f, 0.9f);
            sr.sortingLayerID = sortingLayerId;
            sr.sortingOrder   = sortingOrder;

            var drift = new Vector3(-Random.Range(0.3f, 0.6f), Random.Range(0.05f, 0.25f), 0f) * cellSize;
            DOTween.Sequence()
                .Join(bubble.transform.DOMove(bubble.transform.position + drift, BubbleLife).SetEase(Ease.OutQuad))
                .Join(bubble.transform.DOScale(0f, BubbleLife).SetEase(Ease.InQuad))
                .Join(sr.DOFade(0f, BubbleLife).SetEase(Ease.InQuad))
                .OnComplete(() => Object.Destroy(bubble))
                .SetLink(bubble);
        }
    }
}
#nullable disable
