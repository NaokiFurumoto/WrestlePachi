#nullable enable
using DG.Tweening;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>
    /// 時限爆弾の爆発。ドカーンと爆発の絵が一気にふくらみ、少し残ってから広がりながら消える。画面も揺れる。
    /// Prefab は使わず実行時に作る。
    /// </summary>
    public static class BombExplosionEffect
    {
        /// <summary>爆発の大きさ（ぷよ何個分か）：出た瞬間／いちばん大きいとき／消えるとき</summary>
        private const float StartSizeInCells = 0.6f;
        private const float PeakSizeInCells  = 3.2f;
        private const float EndSizeInCells   = 3.7f;
        /// <summary>ふくらむ・とどまる・消える時間（秒）</summary>
        private const float GrowTime = 0.14f;
        private const float HoldTime = 0.12f;
        private const float FadeTime = 0.3f;
        /// <summary>ぷよより何段手前に描くか</summary>
        private const int OrderAboveRef = 25;

        /// <summary>pos で爆発させる。sprite が未設定ならオレンジの丸で仮表示する</summary>
        public static void Play(Vector3 pos, float cellSize, Sprite? sprite, SpriteRenderer? sortRef, Transform parent)
        {
            ScreenShakeController.Shake(ShakePreset.Medium);

            var go = new GameObject("BombExplosion");
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : FallbackSkillVisuals.SoftCircleSprite;
            if (sprite == null) sr.color = new Color(1f, 0.55f, 0.15f, 1f);
            if (sortRef != null)
            {
                sr.sortingLayerID = sortRef.sortingLayerID;
                sr.sortingOrder   = sortRef.sortingOrder + OrderAboveRef;
            }

            var unit = cellSize / sr.sprite.bounds.size.x;
            go.transform.localScale = Vector3.one * (unit * StartSizeInCells);
            DOTween.Sequence()
                .Append(go.transform.DOScale(unit * PeakSizeInCells, GrowTime).SetEase(Ease.OutBack))
                .AppendInterval(HoldTime)
                .Append(go.transform.DOScale(unit * EndSizeInCells, FadeTime).SetEase(Ease.OutQuad))
                .Join(sr.DOFade(0f, FadeTime).SetEase(Ease.InQuad))
                .OnComplete(() => Object.Destroy(go))
                .SetLink(go);
        }
    }
}
#nullable disable
