#nullable enable
using System.Threading;
using App.Effects;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 連鎖コンボ演出。2連鎖以上で GameMainController から Show() を呼ぶ。
    ///
    /// Prefab 構成:
    ///   ComboView (root)
    ///   ├── TextMeshPro  (_numberText)  ← 連鎖数のみ。App/ComboNumber マテリアル適用
    ///   └── SpriteRenderer (_comboImage) ← COMBO!! 装飾画像
    /// </summary>
    public sealed class ComboView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro     _numberText  = default!;
        [SerializeField] private SpriteRenderer? _comboImage;

        [Header("アニメーション")]
        [SerializeField] private float _displayDuration = 0.8f;
        [SerializeField] private float _fadeOutDuration = 0.4f;

        [Header("花火エフェクト")]
        [SerializeField] private ComboFireworkEffect? _fireworkEffect;

        [Header("表示位置（親からのオフセット）")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -1f);

        private CancellationTokenSource? _cts;

        private void Awake() => SetAlpha(0f);

        /// <param name="worldPos">消えたぷよ群の重心（ワールド座標）。_offset を加算して最終位置とする。</param>
        public void Show(int chainCount, Vector3 worldPos)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            transform.position   = worldPos + _offset;
            transform.localScale = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            _numberText.text        = chainCount.ToString();
            SetAlpha(1f);

            _fireworkEffect?.PlayAsync(chainCount, _cts.Token).Forget();

            AnimateAsync(chainCount, _cts.Token).Forget();
        }

        private async UniTaskVoid AnimateAsync(int chainCount, CancellationToken ct)
        {
            // 連鎖数が多いほど大きく出る（2連鎖=1.6、3連鎖=1.75、上限2.2）
            var peakScale = Mathf.Min(1.3f + chainCount * 0.15f, 2.2f);

            // パンチイン + 回転ゆらぎ を同時開始
            var tScale  = transform.DOScale(peakScale, 0.12f).SetEase(Ease.OutBack);
            var tRotate = transform.DOPunchRotation(Vector3.forward * 10f, 0.35f, vibrato: 1, elasticity: 0f);
            using var r1 = ct.Register(() => { tScale.Kill(); tRotate.Kill(); });
            await tScale.AsyncWaitForCompletion();
            if (ct.IsCancellationRequested) return;

            // 等倍に落ち着く
            var tSettle = transform.DOScale(1f, 0.1f).SetEase(Ease.OutSine);
            using var r2 = ct.Register(() => tSettle.Kill());
            await tSettle.AsyncWaitForCompletion();
            if (ct.IsCancellationRequested) return;

            // 表示待機（その場に固定）
            if (await UniTask.Delay((int)(_displayDuration * 1000), cancellationToken: ct)
                    .SuppressCancellationThrow()) return;

            // 弾けて消える：その場で膨らんで即消滅
            var seq = DOTween.Sequence();
            seq.Append(transform.DOScale(1.5f, 0.1f).SetEase(Ease.OutQuad));
            seq.Append(transform.DOScale(0f, 0.12f).SetEase(Ease.InQuad));
            seq.Insert(0.1f, DOTween.To(() => _numberText.alpha, v => _numberText.alpha = v, 0f, 0.12f).SetEase(Ease.InQuad));
            if (_comboImage != null)
                seq.Insert(0.1f, _comboImage.DOFade(0f, 0.12f).SetEase(Ease.InQuad));
            using var r3 = ct.Register(() => seq.Kill());
            await seq.AsyncWaitForCompletion();
            if (ct.IsCancellationRequested) return;

            SetAlpha(0f);
        }

        private void SetAlpha(float a)
        {
            _numberText.alpha = a;
            if (_comboImage != null)
            {
                var c = _comboImage.color;
                c.a               = a;
                _comboImage.color = c;
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
#nullable disable
