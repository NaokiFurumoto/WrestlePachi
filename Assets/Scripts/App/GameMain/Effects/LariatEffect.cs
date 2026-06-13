#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Effects
{
    /// <summary>
    /// ラリアット用スプライトアニメーションエフェクト。
    /// Instantiate 後に PlayAsync(ct) を呼ぶと
    /// 16フレームコマ送り → フェードアウト → 自己 Destroy する。
    /// Frames・Material・各パラメータは Prefab 側で設定する。
    /// </summary>
    public sealed class LariatEffect : MonoBehaviour
    {
        [SerializeField] private Sprite[]  _frames   = System.Array.Empty<Sprite>();
        [SerializeField] private Material? _material;
        [SerializeField] private string    _sortingLayerName = "Effect";
        [SerializeField] private int       _sortingOrder     = 0;

        [Header("パラメータ")]
        [SerializeField] private float   _fps          = 24f;
        [SerializeField] private Vector2 _scale        = new Vector2(3.0f, 3.0f);
        [SerializeField] private float   _fadeDuration = 0.3f;

        public async UniTaskVoid PlayAsync(CancellationToken ct)
        {
            if (_frames.Length == 0) { Destroy(gameObject); return; }

            transform.localScale = new Vector3(_scale.x, _scale.y, 1f);

            var sr             = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite           = _frames[0];
            sr.material         = _material;
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder     = _sortingOrder;

            // コマ送り
            var interval = Mathf.RoundToInt(1000f / _fps);
            foreach (var frame in _frames)
            {
                sr.sprite = frame;
                var cancelled = await UniTask.Delay(interval, cancellationToken: ct)
                    .SuppressCancellationThrow();
                if (cancelled) { Destroy(gameObject); return; }
            }

            // フェードアウト
            var fadeTween = sr.DOFade(0f, _fadeDuration).SetEase(Ease.InQuad);
            using (ct.Register(() => fadeTween.Kill()))
                await fadeTween.AsyncWaitForCompletion();

            Destroy(gameObject);
        }
    }
}
#nullable disable
