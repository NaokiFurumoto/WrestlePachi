#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Effects
{
    /// <summary>
    /// ストンピング用スタンプ落下エフェクト（1枚構成）。
    /// 上から落下 → 着地時スケールアップ＋フェードアウト → 自己 Destroy。
    /// </summary>
    public sealed class StompEffect : MonoBehaviour
    {
        [SerializeField] private Sprite?   _sprite;
        [SerializeField] private Material? _material;
        [SerializeField] private string    _sortingLayerName = "Effect";
        [SerializeField] private int       _sortingOrder     = 0;

        [Header("パラメータ")]
        [SerializeField] private float _dropHeight    = 5f;
        [SerializeField] private float _dropDuration  = 0.15f;
        [SerializeField] private float _landScale     = 2.0f;
        [SerializeField] private float _landDuration  = 0.25f;
        [SerializeField] private float _stompScale    = 1.2f;

        public async UniTaskVoid PlayAsync(CancellationToken ct)
        {
            var landPos = transform.position;

            var go = new GameObject("FX_Stomp");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.position   = landPos + Vector3.up * _dropHeight;
            go.transform.localScale = Vector3.one * _stompScale;

            var sr             = go.AddComponent<SpriteRenderer>();
            sr.sprite           = _sprite;
            sr.material         = _material;
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder     = _sortingOrder;

            // 落下
            var dropTween = go.transform.DOMove(landPos, _dropDuration).SetEase(Ease.InQuart);
            using (ct.Register(() => dropTween.Kill()))
                await dropTween.AsyncWaitForCompletion();

            if (ct.IsCancellationRequested) { Destroy(gameObject); return; }

            // 着地：スケールアップ＋フェードアウト
            var landScale = _stompScale * _landScale;
            var landSeq = DOTween.Sequence()
                .Join(go.transform.DOScale(landScale, _landDuration).SetEase(Ease.OutQuad))
                .Join(sr.DOFade(0f, _landDuration).SetEase(Ease.InQuad));

            using (ct.Register(() => landSeq.Kill()))
                await landSeq.AsyncWaitForCompletion();

            Destroy(gameObject);
        }
    }
}
#nullable disable
