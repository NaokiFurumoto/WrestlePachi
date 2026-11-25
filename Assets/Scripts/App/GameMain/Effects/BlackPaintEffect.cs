#nullable enable
using System;
using App.Puyo;
using DG.Tweening;
using UnityEngine;

namespace App.Effects
{
    /// <summary>
    /// 敵スキル「黒保留強化」＝黒塗りの演出。黒い小悪魔が左上から飛んできて、保留玉を筆でベタッと2回なでて
    /// 黒く塗り（1回目でpaintNowを呼んで玉を黒保留の絵に変える）、黒いしぶきを散らしてケケケと揺れ、右上へ飛び去る。
    /// Prefab は使わず実行時に作る。
    /// </summary>
    public static class BlackPaintEffect
    {
        /// <summary>小悪魔の大きさ（保留玉の幅の何倍か）</summary>
        private const float DevilSizeByBall = 1.9f;
        /// <summary>保留玉より何段手前に描くか</summary>
        private const int SortingOrderAboveBall = 10;
        private static readonly Color PaintColor    = new(0.06f, 0.02f, 0.1f, 1f);
        private static readonly Color FallbackDevil = new(0.18f, 0.1f, 0.25f, 1f);

        /// <summary>ball を黒く塗る。paintNow は筆が玉をなでた瞬間に呼ぶ（玉を黒保留の絵に変える）</summary>
        public static void Play(SpriteRenderer ball, Sprite? devilSprite, Action paintNow)
        {
            var center = ball.bounds.center;
            var w      = Mathf.Max(ball.bounds.size.x, 0.1f);

            var go = new GameObject("BlackPaintDevil");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite         = devilSprite != null ? devilSprite : FallbackSkillVisuals.SoftCircleSprite;
            if (devilSprite == null) sr.color = FallbackDevil;
            sr.sortingLayerID = ball.sortingLayerID;
            sr.sortingOrder   = ball.sortingOrder + SortingOrderAboveBall;
            var scale = w * DevilSizeByBall / Mathf.Max(sr.sprite.bounds.size.x, 0.0001f);
            go.transform.localScale = Vector3.one * scale;

            // 筆先（絵の右下）が玉にかかる位置で止まる
            var stop  = center + new Vector3(-w * 0.75f, w * 0.35f, 0f);
            var start = center + new Vector3(-w * 3.5f, w * 3.0f, 0f);
            var leave = center + new Vector3(w * 3.5f, w * 3.2f, 0f);
            go.transform.position = start;

            var t = go.transform;
            DOTween.Sequence()
                // 飛んでくる
                .Append(t.DOJump(stop, w * 0.6f, 1, 0.4f).SetEase(Ease.OutQuad))
                // 1回目：筆を振り下ろす → 塗れる
                .Append(t.DORotate(new Vector3(0f, 0f, 18f), 0.08f))
                .Append(t.DORotate(new Vector3(0f, 0f, -28f), 0.1f).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    paintNow();
                    SpawnSplash(center, w, sr.sortingLayerID, sr.sortingOrder - 1);
                    ScreenShakeController.Shake(ShakePreset.Light);
                })
                // 2回目：念押しでもうひとなで
                .Append(t.DORotate(new Vector3(0f, 0f, 14f), 0.1f))
                .Append(t.DORotate(new Vector3(0f, 0f, -20f), 0.1f).SetEase(Ease.InQuad))
                .AppendCallback(() => SpawnSplash(center, w * 0.7f, sr.sortingLayerID, sr.sortingOrder - 1))
                .Append(t.DORotate(Vector3.zero, 0.1f))
                // ケケケ：小刻みに上下して笑う
                .Append(t.DOPunchPosition(new Vector3(0f, w * 0.18f, 0f), 0.45f, 8, 0.6f))
                // 飛び去る
                .Append(t.DOMove(leave, 0.4f).SetEase(Ease.InQuad))
                .Join(sr.DOFade(0f, 0.4f).SetEase(Ease.InQuad))
                .OnComplete(() => UnityEngine.Object.Destroy(go))
                .SetLink(go);
        }

        /// <summary>黒いペンキのしぶきを玉のまわりに散らす</summary>
        private static void SpawnSplash(Vector3 center, float w, int layerId, int order)
        {
            for (var i = 0; i < 6; i++)
            {
                var go = new GameObject("BlackPaintSplash");
                go.transform.position   = center;
                go.transform.localScale = Vector3.one * w * UnityEngine.Random.Range(0.18f, 0.32f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite         = FallbackSkillVisuals.SoftCircleSprite;
                sr.color          = PaintColor;
                sr.sortingLayerID = layerId;
                sr.sortingOrder   = order;
                var angle = (i / 6f + UnityEngine.Random.Range(-0.06f, 0.06f)) * Mathf.PI * 2f;
                var to    = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * w * UnityEngine.Random.Range(0.55f, 0.9f);
                DOTween.Sequence()
                    .Append(go.transform.DOJump(to, w * 0.25f, 1, 0.3f))
                    .Join(go.transform.DOScale(go.transform.localScale * 0.5f, 0.3f))
                    .Insert(0.15f, sr.DOFade(0f, 0.25f))
                    .OnComplete(() => UnityEngine.Object.Destroy(go))
                    .SetLink(go);
            }
        }
    }
}
#nullable disable
