#nullable enable
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace GameSys
{
    public class ViewAnimation : MonoBehaviour
    {
        public enum AnimType
        {
            Pop,
            Slide,
            Fade,
            Stamp
        }

        [SerializeField] private AnimType _type = AnimType.Pop;
        [SerializeField] private CanvasGroup? _canvasGroup;

        // 💡 Transform のままでOK！
        [SerializeField] private Transform? _windowRect;

        [Header("Settings")]
        [SerializeField] private float _duration = 0.3f;
        [SerializeField] private Ease _ease = Ease.OutBack;

        public void SetupBeforeOpen()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0;

            // 💡 UIとして扱うために RectTransform にキャスト
            var rect = _windowRect as RectTransform;

            switch (_type)
            {
                case AnimType.Pop:
                    if (_windowRect != null) _windowRect.localScale = Vector3.one * 0.7f;
                    break;
                case AnimType.Slide:
                    // 💡 rect経由で anchoredPosition を操作
                    if (rect != null) rect.anchoredPosition += new Vector2(0, -500);
                    break;
                case AnimType.Stamp:
                    if (_windowRect != null) _windowRect.localScale = Vector3.one * 1.5f;
                    break;
            }
        }

        public async UniTask PlayOpenAsync()
        {
            SetupBeforeOpen();
            var seq = DOTween.Sequence();
            var rect = _windowRect as RectTransform;

            if (_canvasGroup != null) seq.Join(_canvasGroup.DOFade(1f, _duration));

            switch (_type)
            {
                case AnimType.Pop:
                    if (_windowRect != null) seq.Join(_windowRect.DOScale(1f, _duration).SetEase(_ease));
                    break;
                case AnimType.Slide:
                    // 💡 DOAnchorPosY は RectTransform に対して呼ぶ
                    if (rect != null) seq.Join(rect.DOAnchorPosY(0, _duration).SetEase(Ease.OutCubic));
                    break;
                case AnimType.Fade:
                    break;
                case AnimType.Stamp:
                    if (_windowRect != null) seq.Join(_windowRect.DOScale(1f, _duration).SetEase(Ease.OutBounce));
                    break;
            }

            await seq.Play().AsyncWaitForCompletion();
        }

        public async UniTask PlayCloseAsync()
        {
            var seq = DOTween.Sequence();
            var rect = _windowRect as RectTransform;

            if (_canvasGroup != null) seq.Join(_canvasGroup.DOFade(0f, _duration * 0.7f));

            switch (_type)
            {
                case AnimType.Slide:
                    if (rect != null) seq.Join(rect.DOAnchorPosY(-500, _duration * 0.7f).SetEase(Ease.InCubic));
                    break;
                default:
                    if (_windowRect != null) seq.Join(_windowRect.DOScale(0.8f, _duration * 0.7f).SetEase(Ease.InQuad));
                    break;
            }

            await seq.Play().AsyncWaitForCompletion();
        }
    }
}