#nullable enable
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace App
{
    /// <summary>
    /// 画面全体のポストプロセス演出を一元管理するコントローラー。
    /// GlobalVolume と同じ GameObject に置く。シーン内に1つだけ存在する Singleton。
    /// スキル発動・クリア・ゲームオーバー・タイマー警告などの Bloom / ColorGrading 演出を担う。
    /// Volume は Awake 時に profile を複製するので元アセットを汚染しない。
    /// </summary>
    public sealed class ScreenEffectController : MonoBehaviour
    {
        public static ScreenEffectController? Instance { get; private set; }

        [SerializeField] private Volume _volume = default!;

        [Header("Bloom 強度")]
        [SerializeField] private float _baseIntensity = 0f;
        [SerializeField] private float _peakIntensity = 10f;

        [Header("アニメーション時間（秒）")]
        [SerializeField] private float _riseTime = 0.08f;
        [SerializeField] private float _holdTime = 0.15f;
        [SerializeField] private float _fadeTime = 0.70f;

        [Header("警告点滅（タイマー残り少ない時）")]
        [SerializeField] private float _warningIntensity = 3f;
        [SerializeField] private float _warningInterval  = 0.5f;

        [Header("へそ入賞（きらっ）")]
        [SerializeField] private float _hesoBloomPeak  = 6f;
        [SerializeField] private float _hesoChromaPeak = 0.6f;
        [SerializeField] private float _hesoRise       = 0.03f;
        [SerializeField] private float _hesoFade       = 0.18f;

        [Header("デバッグ確認用（読み取り専用）")]
        [SerializeField] private float _currentIntensity;

        private Bloom?               _bloom;
        private ColorAdjustments?    _colorAdj;
        private ChromaticAberration? _chromaticAberration;
        private Tween?               _tween;
        private Tween?               _warningTween;
        private Tween?               _chromaTween;

        // ── ライフサイクル ────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _volume.profile = Instantiate(_volume.profile);

            if (!_volume.profile.TryGet(out _bloom))
                Debug.LogWarning("[ScreenEffectController] Volume Profile に Bloom override がありません。");

            _volume.profile.TryGet(out _colorAdj);
            _volume.profile.TryGet(out _chromaticAberration);

            _currentIntensity = _baseIntensity;
            _bloom?.intensity.Override(_baseIntensity);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            _warningTween?.Kill();
            _chromaTween?.Kill();
            if (Instance == this) Instance = null;
        }

        // ── 静的 API ─────────────────────────────────────────────────

        /// <summary>へそ入賞：金色 Bloom + Chromatic Aberration スパイク。</summary>
        public static void PlayHeso()
            => Instance?.PlayHesoEffect();

        /// <summary>スキル種別に対応した色で Bloom バーストを再生する。黒保留は演出なし。</summary>
        public static void PlaySkill(HoldType holdType)
        {
            if (holdType == HoldType.Black) return;
            Instance?.PlayBloomBurst(SkillColor(holdType));
        }

        /// <summary>任意の色で Bloom バーストを再生する。</summary>
        public static void Play(Color tintColor)
            => Instance?.PlayBloomBurst(tintColor);

        /// <summary>クリア時のホワイトフラッシュ演出。</summary>
        public static void PlayClear()
            => Instance?.PlayBloomBurst(Color.white, peakOverride: 15f, holdOverride: 0.3f);

        /// <summary>ゲームオーバー時の暗転演出。</summary>
        public static void PlayGameOver()
            => Instance?.PlayDarken();

        /// <summary>タイマー警告の点滅を開始する。</summary>
        public static void StartWarning()
            => Instance?.PlayWarningLoop();

        /// <summary>タイマー警告の点滅を停止する。</summary>
        public static void StopWarning()
            => Instance?.StopWarningLoop();

        // ── デバッグ ─────────────────────────────────────────────────

#if UNITY_EDITOR
        [ContextMenu("テスト：へそ入賞")] void DbgHeso()    => PlayHesoEffect();
        [ContextMenu("テスト：Red")]     void DbgRed()     => PlayBloomBurst(SkillColor(HoldType.Red));
        [ContextMenu("テスト：Yellow")]  void DbgYellow()  => PlayBloomBurst(SkillColor(HoldType.Yellow));
        [ContextMenu("テスト：Green")]   void DbgGreen()   => PlayBloomBurst(SkillColor(HoldType.Green));
        [ContextMenu("テスト：Blue")]    void DbgBlue()    => PlayBloomBurst(SkillColor(HoldType.Blue));
        [ContextMenu("テスト：Purple")]  void DbgPurple()  => PlayBloomBurst(SkillColor(HoldType.Purple));
        [ContextMenu("テスト：Rainbow")] void DbgRainbow() => PlayBloomBurst(SkillColor(HoldType.Rainbow));
        [ContextMenu("テスト：クリア")]  void DbgClear()   => PlayClear();
        [ContextMenu("テスト：ゲームオーバー")] void DbgGameOver() => PlayGameOver();
        [ContextMenu("テスト：警告点滅開始")]   void DbgWarningOn()  => StartWarning();
        [ContextMenu("テスト：警告点滅停止")]   void DbgWarningOff() => StopWarning();
#endif

        // ── 内部処理 ─────────────────────────────────────────────────

        private void PlayBloomBurst(Color tintColor, float peakOverride = -1f, float holdOverride = -1f)
        {
            if (_bloom == null) return;

            var peak = peakOverride > 0f ? peakOverride : _peakIntensity;
            var hold = holdOverride > 0f ? holdOverride : _holdTime;

            _tween?.Kill();
            _warningTween?.Kill();
            _bloom.tint.Override(tintColor);

            _tween = DOTween.Sequence()
                .Append(DOTween.To(
                    () => _currentIntensity,
                    v  => { _currentIntensity = v; _bloom.intensity.Override(v); },
                    peak, _riseTime
                ).SetEase(Ease.OutQuad))
                .AppendInterval(hold)
                .Append(DOTween.To(
                    () => _currentIntensity,
                    v  => { _currentIntensity = v; _bloom.intensity.Override(v); },
                    _baseIntensity, _fadeTime
                ).SetEase(Ease.InOutCubic))
                .OnComplete(() =>
                {
                    _currentIntensity = _baseIntensity;
                    _bloom.intensity.Override(_baseIntensity);
                });
        }

        private void PlayDarken()
        {
            if (_colorAdj == null) return;
            _tween?.Kill();
            _tween = DOTween
                .To(
                    () => _colorAdj.postExposure.value,
                    v  => _colorAdj.postExposure.Override(v),
                    -3f, 1.0f
                )
                .SetEase(Ease.InCubic);
        }

        private void PlayHesoEffect()
        {
            // Bloom: 瞬間点灯 → 素早くフェード
            _tween?.Kill();
            _bloom?.tint.Override(Color.white);
            _tween = DOTween.Sequence()
                .Append(DOTween.To(
                    () => _currentIntensity,
                    v  => { _currentIntensity = v; _bloom?.intensity.Override(v); },
                    _hesoBloomPeak, _hesoRise
                ).SetEase(Ease.OutQuint))
                .Append(DOTween.To(
                    () => _currentIntensity,
                    v  => { _currentIntensity = v; _bloom?.intensity.Override(v); },
                    _baseIntensity, _hesoFade
                ).SetEase(Ease.OutQuad))
                .OnComplete(() =>
                {
                    _currentIntensity = _baseIntensity;
                    _bloom?.intensity.Override(_baseIntensity);
                });

            // 色収差: 瞬間ズレ → 素早く戻る
            if (_chromaticAberration == null) return;
            _chromaTween?.Kill();
            _chromaticAberration.intensity.Override(0f);
            _chromaTween = DOTween.Sequence()
                .Append(DOTween.To(
                    () => _chromaticAberration.intensity.value,
                    v  => _chromaticAberration.intensity.Override(v),
                    _hesoChromaPeak, _hesoRise
                ).SetEase(Ease.OutQuint))
                .Append(DOTween.To(
                    () => _chromaticAberration.intensity.value,
                    v  => _chromaticAberration.intensity.Override(v),
                    0f, _hesoFade
                ).SetEase(Ease.OutQuad));
        }

        private void PlayWarningLoop()
        {
            if (_bloom == null) return;
            _warningTween?.Kill();
            _bloom.tint.Override(new Color(1f, 0.2f, 0.1f));
            _warningTween = DOTween
                .To(
                    () => _currentIntensity,
                    v  => { _currentIntensity = v; _bloom.intensity.Override(v); },
                    _warningIntensity, _warningInterval
                )
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void StopWarningLoop()
        {
            if (_bloom == null) return;
            _warningTween?.Kill();
            _warningTween = null;
            _bloom.tint.Override(Color.white);
            _currentIntensity = _baseIntensity;
            _bloom.intensity.Override(_baseIntensity);
        }

        // ── スキル種別→発光色 ─────────────────────────────────────

        private static Color SkillColor(HoldType type) => type switch
        {
            HoldType.Red     => new Color(1.0f, 0.20f, 0.08f),
            HoldType.Yellow  => new Color(1.0f, 0.88f, 0.10f),
            HoldType.Green   => new Color(0.10f, 1.0f, 0.28f),
            HoldType.Blue    => new Color(0.15f, 0.50f, 1.0f),
            HoldType.Purple  => new Color(0.65f, 0.10f, 1.0f),
            HoldType.Black   => new Color(0.50f, 0.50f, 0.60f),
            HoldType.Rainbow => Color.white,
            _                => Color.white,
        };
    }
}
#nullable disable
