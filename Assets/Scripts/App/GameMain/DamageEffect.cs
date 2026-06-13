#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// ダメージ数値演出。
    /// BG 画像がズームアウト＋着地シェイク → 数値が上に浮かびフェードアウト。
    /// GameMainHudView から PlayAsync を呼ぶ。
    /// </summary>
    public sealed class DamageEffect : MonoBehaviour
    {
        [SerializeField] private Image    _bg   = default!;
        [SerializeField] private TMP_Text _text = default!;

        [Header("BG アニメーション")]
        [SerializeField] private float _startScale     = 2.5f;
        [SerializeField] private float _zoomDuration   = 0.18f;
        [SerializeField] private float _shakeDuration  = 0.15f;
        [SerializeField] private float _shakeStrength  = 0.3f;
        [SerializeField] private float _bgFadeDuration = 0.35f;

        [Header("テキスト アニメーション")]
        [SerializeField] private float _textDelay    = 0.12f;
        [SerializeField] private float _textRise     = 80f;
        [SerializeField] private float _textDuration = 0.65f;

        private Vector2 _textOrigin;

        private void Awake()
        {
            _textOrigin = _text.rectTransform.anchoredPosition;
            gameObject.SetActive(false);
        }

        public async UniTaskVoid PlayAsync(int damage, CancellationToken ct)
        {
            KillAll();

            _text.text  = damage.ToString();
            _bg.transform.localScale             = Vector3.one * _startScale;
            _bg.color                            = Color.white;
            _text.alpha                          = 0f;
            _text.rectTransform.anchoredPosition = _textOrigin;
            gameObject.SetActive(true);

            // ① BG：ズームアウト（大→等倍、OutBack でバウンド）
            {
                var t = _bg.transform.DOScale(1f, _zoomDuration).SetEase(Ease.OutBack);
                using var r = ct.Register(() => t.Kill());
                await t.AsyncWaitForCompletion();
            }
            if (ct.IsCancellationRequested) { Hide(); return; }

            // ② BG：着地シェイク
            _bg.transform.DOShakeScale(_shakeDuration, _shakeStrength);

            // ③ テキスト：少し待って出現
            if (await UniTask.Delay(
                    Mathf.RoundToInt(_textDelay * 1000), cancellationToken: ct
                ).SuppressCancellationThrow()) { Hide(); return; }

            // ④ テキスト：浮かびあがり＋フェードイン
            _text.rectTransform
                .DOAnchorPos(_textOrigin + Vector2.up * _textRise, _textDuration)
                .SetEase(Ease.OutCubic);
            DOTween.To(() => _text.alpha, v => _text.alpha = v, 1f, 0.1f);

            // ⑤ BG：フェードアウト
            _bg.DOFade(0f, _bgFadeDuration);

            // ⑥ テキスト：後半フェードアウト
            if (await UniTask.Delay(
                    Mathf.RoundToInt((_textDuration - 0.25f) * 1000), cancellationToken: ct
                ).SuppressCancellationThrow()) { Hide(); return; }

            DOTween.To(() => _text.alpha, v => _text.alpha = v, 0f, 0.25f);

            await UniTask.Delay(250, cancellationToken: ct).SuppressCancellationThrow();
            Hide();
        }

        private void KillAll()
        {
            _bg.transform.DOKill();
            _bg.DOKill();
            DOTween.Kill(_text);
            _text.rectTransform.DOKill();
        }

        private void Hide()
        {
            KillAll();
            gameObject.SetActive(false);
            _text.rectTransform.anchoredPosition = _textOrigin;
        }
    }
}
#nullable disable
