#nullable enable
using DG.Tweening;
using UnityEngine;

namespace App
{
    /// <summary>
    /// ラジアルブラーマテリアルの制御。ScreenEffectController と同じ GameObject に置く。
    /// </summary>
    public sealed class RadialBlurController : MonoBehaviour
    {
        [SerializeField] private Material? _material;

        [SerializeField] private float _peak = 0.25f;
        [SerializeField] private float _rise = 0.03f;
        [SerializeField] private float _fade = 2.0f;

        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private Tween? _tween;

        private void Awake()
            => _material?.SetFloat(IntensityId, 0f);

        private void OnDestroy()
            => _tween?.Kill();

        public void Play(float peakOverride = -1f, float fadeOverride = -1f)
        {
            if (_material == null) return;

            var peak = peakOverride > 0f ? peakOverride : _peak;
            var fade = fadeOverride > 0f ? fadeOverride : _fade;

            _tween?.Kill();
            _material.SetFloat(IntensityId, 0f);

            _tween = DOTween.Sequence()
                .Append(DOTween.To(
                    () => _material.GetFloat(IntensityId),
                    v  => _material.SetFloat(IntensityId, v),
                    peak, _rise).SetEase(Ease.OutQuad))
                .Append(DOTween.To(
                    () => _material.GetFloat(IntensityId),
                    v  => _material.SetFloat(IntensityId, v),
                    0f, fade).SetEase(Ease.InCubic))
                .OnComplete(() => _material.SetFloat(IntensityId, 0f))
                .SetUpdate(true);
        }

#if UNITY_EDITOR
        [ContextMenu("テスト：ラジアルブラー")] void Dbg() => Play();
#endif
    }
}
#nullable disable
