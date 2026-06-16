#nullable enable
using DG.Tweening;
using UnityEngine;

namespace App
{
    public enum ShakePreset { Light, Medium, Heavy }

    /// <summary>
    /// カメラシェイクを一元管理するシングルトン。GameScene 専用。
    /// プリセット指定・カスタム指定どちらでも1行で呼べる。
    /// SetUpdate(true) により timeScale=0 中でも動作する。
    /// </summary>
    public sealed class ScreenShakeController : MonoBehaviour
    {
        public static ScreenShakeController? Instance { get; private set; }

        [Header("シェイク対象カメラ（未設定時は Camera.main を使用）")]
        [SerializeField] private Camera? _camera;

        [Header("プリセット：Light（軽い振動）")]
        [SerializeField] private float _lightStrength  = 0.08f;
        [SerializeField] private float _lightDuration  = 0.25f;
        [SerializeField] private int   _lightVibrato   = 12;

        [Header("プリセット：Medium（中程度）")]
        [SerializeField] private float _mediumStrength = 0.18f;
        [SerializeField] private float _mediumDuration = 0.35f;
        [SerializeField] private int   _mediumVibrato  = 15;

        [Header("プリセット：Heavy（撃破・大ダメージ）")]
        [SerializeField] private float _heavyStrength  = 0.30f;
        [SerializeField] private float _heavyDuration  = 0.50f;
        [SerializeField] private int   _heavyVibrato   = 20;

        private Tween? _tween;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            if (Instance == this) Instance = null;
        }

        // ── 静的 API ─────────────────────────────────────────────────

        /// <summary>プリセットでシェイク。</summary>
        public static void Shake(ShakePreset preset)
            => Instance?.DoShakePreset(preset);

        /// <summary>強度・時間を直接指定してシェイク。</summary>
        public static void Shake(float strength, float duration, int vibrato = 15)
            => Instance?.DoShake(strength, duration, vibrato);

        // ── 内部処理 ─────────────────────────────────────────────────

        private void DoShakePreset(ShakePreset preset)
        {
            var (s, d, v) = preset switch
            {
                ShakePreset.Light  => (_lightStrength,  _lightDuration,  _lightVibrato),
                ShakePreset.Medium => (_mediumStrength, _mediumDuration, _mediumVibrato),
                ShakePreset.Heavy  => (_heavyStrength,  _heavyDuration,  _heavyVibrato),
                _                  => (_mediumStrength, _mediumDuration, _mediumVibrato),
            };
            DoShake(s, d, v);
        }

        private void DoShake(float strength, float duration, int vibrato)
        {
            var cam = _camera != null ? _camera : Camera.main;
            if (cam == null) return;

            _tween?.Kill();
            var origin = cam.transform.localPosition;

            _tween = cam.transform
                .DOShakePosition(duration, strength, vibrato, randomness: 90f, fadeOut: true)
                .SetUpdate(true)
                .OnKill(() => cam.transform.localPosition = origin);
        }

#if UNITY_EDITOR
        [ContextMenu("テスト：Light")]  void DbgLight()  => DoShakePreset(ShakePreset.Light);
        [ContextMenu("テスト：Medium")] void DbgMedium() => DoShakePreset(ShakePreset.Medium);
        [ContextMenu("テスト：Heavy")]  void DbgHeavy()  => DoShakePreset(ShakePreset.Heavy);
#endif
    }
}
#nullable disable
