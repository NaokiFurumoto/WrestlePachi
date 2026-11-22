#nullable enable
using DG.Tweening;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>
    /// 岩が割れたときの破片と砂ぼこり。小さな岩のかけらが回りながら放物線を描いて飛び散り、
    /// 薄い砂ぼこりがふわっと広がって消える。Prefab は使わず実行時に作る。
    /// </summary>
    public static class RockDebrisEffect
    {
        /// <summary>破片の大きさ（ぷよ何個分か）</summary>
        private const float ChipSizeMin = 0.16f;
        private const float ChipSizeMax = 0.3f;
        /// <summary>破片が飛んでいる時間（秒）</summary>
        private const float ChipLife = 0.55f;
        /// <summary>破片・砂ぼこりをぷよより何段手前に描くか</summary>
        private const int OrderAboveRef = 10;

        /// <summary>
        /// pos から破片を count 個飛ばす。power が大きいほど遠くまで飛び、砂ぼこりも大きい。
        /// chipSprite が未設定なら灰色の四角を破片にする。sortRef と同じレイヤーの手前に描く。
        /// </summary>
        public static void Spawn(Vector3 pos, float cellSize, Sprite? chipSprite, SpriteRenderer? sortRef,
                                 Transform parent, int count, float power)
        {
            for (var i = 0; i < count; i++)
            {
                // 全方向にだいたい均等に散らす（少しずつばらつかせる）
                var angle = (360f * i / count + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
                SpawnChip(pos, cellSize, chipSprite, sortRef, parent, Mathf.Cos(angle), power);
            }
            SpawnDust(pos, cellSize, sortRef, parent, power);
        }

        private static void SpawnChip(Vector3 pos, float cellSize, Sprite? chipSprite, SpriteRenderer? sortRef,
                                      Transform parent, float dirX, float power)
        {
            var go = new GameObject("RockChip");
            go.transform.SetParent(parent, true);
            go.transform.position = pos + new Vector3(dirX * 0.25f, Random.Range(-0.1f, 0.2f), 0f) * cellSize;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = chipSprite != null ? chipSprite : FallbackSkillVisuals.SquareSprite;
            if (chipSprite == null) sr.color = FallbackSkillVisuals.Rock;
            ApplySorting(sr, sortRef, OrderAboveRef);

            var size  = Random.Range(ChipSizeMin, ChipSizeMax) * cellSize;
            var scale = size / sr.sprite.bounds.size.x;
            go.transform.localScale = Vector3.one * scale;

            var end  = go.transform.position + new Vector3(dirX * Random.Range(0.6f, 1.1f) * power, -Random.Range(0.4f, 0.9f), 0f) * cellSize;
            var jump = Random.Range(0.35f, 0.7f) * power * cellSize;
            var spin = Random.Range(-540f, 540f);
            DOTween.Sequence()
                .Append(go.transform.DOJump(end, jump, 1, ChipLife).SetEase(Ease.Linear))
                .Join(go.transform.DORotate(new Vector3(0f, 0f, spin), ChipLife, RotateMode.FastBeyond360))
                .Join(sr.DOFade(0f, ChipLife * 0.4f).SetDelay(ChipLife * 0.6f))
                .OnComplete(() => Object.Destroy(go))
                .SetLink(go);
        }

        /// <summary>薄い砂ぼこり：やわらかい丸が数個、広がりながら消える</summary>
        private static void SpawnDust(Vector3 pos, float cellSize, SpriteRenderer? sortRef, Transform parent, float power)
        {
            var puffCount = power >= 1f ? 4 : 2;
            for (var i = 0; i < puffCount; i++)
            {
                var go = new GameObject("RockDust");
                go.transform.SetParent(parent, true);
                go.transform.position = pos + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.2f, 0.1f), 0f) * cellSize;
                go.transform.localScale = Vector3.zero;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = FallbackSkillVisuals.SoftCircleSprite;
                sr.color  = new Color(0.78f, 0.74f, 0.7f, 0.7f);
                ApplySorting(sr, sortRef, OrderAboveRef - 1);

                var size = Random.Range(0.5f, 0.8f) * power * cellSize;
                var life = Random.Range(0.4f, 0.55f);
                DOTween.Sequence()
                    .Append(go.transform.DOScale(size, life).SetEase(Ease.OutQuad))
                    .Join(go.transform.DOMove(go.transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0.25f, 0f) * cellSize, life))
                    .Join(sr.DOFade(0f, life).SetEase(Ease.InQuad))
                    .OnComplete(() => Object.Destroy(go))
                    .SetLink(go);
            }
        }

        private static void ApplySorting(SpriteRenderer sr, SpriteRenderer? sortRef, int orderAbove)
        {
            if (sortRef == null) return;
            sr.sortingLayerID = sortRef.sortingLayerID;
            sr.sortingOrder   = sortRef.sortingOrder + orderAbove;
        }
    }
}
#nullable disable
