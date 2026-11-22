#nullable enable
using DG.Tweening;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>
    /// ぷよが焦げるときの煙。灰色〜黒っぽいモクモクした煙が、ふくらみながら立ちのぼって消える。
    /// Prefab は使わず、縁のやわらかい丸を重ねて作る。
    /// </summary>
    public static class BurnSmokeEffect
    {
        /// <summary>煙のかたまりの数・出し終わるまでの時間（秒）・1つが消えるまでの時間（秒）</summary>
        private const int   PuffCount    = 7;
        private const float SpawnSpan    = 0.25f;
        private const float PuffLife     = 0.9f;
        /// <summary>煙の大きさ（ぷよ何個分か）：出た直後と消える直前</summary>
        private const float StartSizeMin = 0.35f;
        private const float StartSizeMax = 0.55f;
        private const float EndSizeScale = 2.2f;
        /// <summary>どれだけ上へ昇るか（ぷよ何個分か）</summary>
        private const float RiseInCells  = 1.4f;

        /// <summary>
        /// pos から煙を立ちのぼらせる。ぷよ（pieceRenderer）より手前に描く。
        /// </summary>
        public static void Spawn(Vector3 pos, float cellSize, SpriteRenderer? pieceRenderer, Transform parent)
        {
            for (var i = 0; i < PuffCount; i++)
            {
                var delay = SpawnSpan * i / PuffCount;
                SpawnPuff(pos, cellSize, pieceRenderer, parent, delay);
            }
        }

        private static void SpawnPuff(Vector3 pos, float cellSize, SpriteRenderer? pieceRenderer, Transform parent, float delay)
        {
            var puff = new GameObject("BurnSmoke");
            puff.transform.SetParent(parent, true);
            puff.transform.position = pos + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.1f, 0.15f), 0f) * cellSize;

            var size = Random.Range(StartSizeMin, StartSizeMax) * cellSize;
            puff.transform.localScale = Vector3.zero;

            var sr = puff.AddComponent<SpriteRenderer>();
            sr.sprite = FallbackSkillVisuals.SoftCircleSprite;
            // 焦げた感じ：根元は黒っぽく、色の濃さを少しずつばらつかせる
            var gray = Random.Range(0.18f, 0.38f);
            sr.color = new Color(gray, gray * 0.95f, gray * 0.9f, 0.85f);
            if (pieceRenderer != null)
            {
                sr.sortingLayerID = pieceRenderer.sortingLayerID;
                sr.sortingOrder   = pieceRenderer.sortingOrder + 5;
            }

            var rise  = new Vector3(Random.Range(-0.35f, 0.35f), RiseInCells, 0f) * cellSize;
            var spin  = Random.Range(-90f, 90f);
            DOTween.Sequence()
                .SetDelay(delay)
                .Append(puff.transform.DOScale(size, 0.12f).SetEase(Ease.OutQuad))
                .Append(puff.transform.DOScale(size * EndSizeScale, PuffLife).SetEase(Ease.OutQuad))
                .Join(puff.transform.DOMove(puff.transform.position + rise, PuffLife).SetEase(Ease.OutCubic))
                .Join(puff.transform.DORotate(new Vector3(0f, 0f, spin), PuffLife))
                .Join(sr.DOColor(new Color(0.45f, 0.45f, 0.45f, 0f), PuffLife).SetEase(Ease.InQuad))
                .OnComplete(() => Object.Destroy(puff))
                .SetLink(puff);
        }
    }
}
#nullable disable
