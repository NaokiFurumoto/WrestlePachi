#nullable enable
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace App
{
    /// <summary>
    /// スキル発動時に Bloom を一時的に強くする Post-process 演出コントローラー。
    /// GlobalVolume と同じ GameObject に置く。シーン内に1つだけ存在する想定の Singleton。
    /// どこからでも BloomEffectController.PlaySkill(...) で呼び出せる。
    /// Volume は Awake 時に profile を複製するので元アセットを汚染しない。
    /// </summary>
    public sealed class BloomEffectController : MonoBehaviour
    {
        public static BloomEffectController? Instance { get; private set; }

        [SerializeField] private Volume _volume = default!;

        [Header("Bloom 強度")]
        [SerializeField] private float _baseIntensity = 0f;   // 通常時（スキル後に戻る値）
        [SerializeField] private float _peakIntensity = 10f;  // スキル発動時のピーク強度

        [Header("アニメーション時間（秒）")]
        [SerializeField] private float _riseTime = 0.08f;  // ピークまでの上昇時間
        [SerializeField] private float _holdTime = 0.15f;  // ピーク保持時間
        [SerializeField] private float _fadeTime = 0.70f;  // 基準値への収束時間

        private Bloom? _bloom;
        private Tween? _tween;

        [Header("デバッグ確認用（読み取り専用）")]
        [SerializeField] private float _currentIntensity;
        [SerializeField] private float _debugBaseIntensity;

        // ── ライフサイクル ────────────────────────────────────────────

        private void Awake()
        {
            // 重複排除：既にインスタンスがあれば自分を破棄して終了
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ランタイム用にプロファイルを複製（アセットを直接書き換えない）
            _volume.profile = Instantiate(_volume.profile);

            if (!_volume.profile.TryGet(out _bloom))
                Debug.LogWarning("[BloomEffectController] Volume Profile に Bloom override がありません。追加してください。");

            _currentIntensity   = _baseIntensity;
            _debugBaseIntensity = _baseIntensity;
            _bloom?.intensity.Override(_baseIntensity);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            if (Instance == this) Instance = null;
        }

        // ── 静的 API（どこからでも呼べる） ──────────────────────────

        /// <summary>スキル種別に対応した色で Bloom バーストを再生する。</summary>
        public static void PlaySkill(HoldType holdType)
            => Instance?.PlayBloomInternal(SkillColor(holdType));

        /// <summary>任意の色で Bloom バーストを再生する。</summary>
        public static void Play(Color tintColor)
            => Instance?.PlayBloomInternal(tintColor);

        // ── デバッグ（Playモード中にInspectorで右クリック → メニューから実行） ──

#if UNITY_EDITOR
        [ContextMenu("テスト：Red")]    void DbgRed()    => PlayBloomInternal(SkillColor(HoldType.Red));
        [ContextMenu("テスト：Yellow")] void DbgYellow() => PlayBloomInternal(SkillColor(HoldType.Yellow));
        [ContextMenu("テスト：Green")]  void DbgGreen()  => PlayBloomInternal(SkillColor(HoldType.Green));
        [ContextMenu("テスト：Blue")]   void DbgBlue()   => PlayBloomInternal(SkillColor(HoldType.Blue));
        [ContextMenu("テスト：Purple")] void DbgPurple() => PlayBloomInternal(SkillColor(HoldType.Purple));
        [ContextMenu("テスト：Rainbow")]void DbgRainbow()=> PlayBloomInternal(SkillColor(HoldType.Rainbow));
#endif

        // ── 内部処理 ─────────────────────────────────────────────────

        private void PlayBloomInternal(Color tintColor)
        {
            if (_bloom == null) return;

            _tween?.Kill();
            _bloom.tint.Override(tintColor);

            // VolumeParameter を直接 getter にすると DOTween が値を読み損ねる場合があるため
            // 中間フィールド _currentIntensity を経由して毎フレーム Override する
            _tween = DOTween.Sequence()
                .Append(DOTween.To(
                    () => _currentIntensity,
                    v  => { _currentIntensity = v; _bloom.intensity.Override(v); },
                    _peakIntensity,
                    _riseTime
                ).SetEase(Ease.OutQuad))
                .AppendInterval(_holdTime)
                .Append(DOTween.To(
                    () => _currentIntensity,
                    v  => { _currentIntensity = v; _bloom.intensity.Override(v); },
                    _baseIntensity,
                    _fadeTime
                ).SetEase(Ease.InOutCubic))
                .OnComplete(() =>
                {
                    // 完了時に確実に基準値へ戻す
                    _currentIntensity = _baseIntensity;
                    _bloom.intensity.Override(_baseIntensity);
                });
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
