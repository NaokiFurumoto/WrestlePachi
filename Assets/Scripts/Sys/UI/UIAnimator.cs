#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GameSys
{
    /// <summary>
    /// RectTransform / Transform を持つ任意のオブジェクトにアタッチして使う汎用 DOTween アニメーターコンポーネント。
    /// Image / Text / RawImage（Graphic 系）や SpriteRenderer があれば Flash・ColorCycle も使用可能。
    /// </summary>
    public class UIAnimator : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // Enum 定義
        // ─────────────────────────────────────────

        public enum AnimType
        {
            Punch,       // パンチ        ：衝撃でスケールが弾けるインパクト演出
            Shake,       // シェイク      ：位置がブルブル震える衝撃演出
            Spin,        // スピン        ：Z軸を中心に回転する
            Flip,        // フリップ      ：X軸スケールを反転させるめくり演出
            Flash,       // フラッシュ    ：透明度を繰り返し点滅させる
            Heartbeat,   // ハートビート  ：鼓動するように拡縮を繰り返す
            Stamp,       // スタンプ      ：バウンスしながら着地するドーン演出
            ColorCycle,  // カラーサイクル：指定した2色の間を循環変化する
        }

        public enum LoopMode
        {
            None,     // ループなし  ：1回だけ再生して止まる
            Restart,  // リスタート  ：最初から繰り返す（無限ループ）
            Yoyo,     // ヨーヨー    ：往復する（無限ループ）
        }

        // ─────────────────────────────────────────
        // 共通設定
        // ─────────────────────────────────────────
        [Header("── 共通設定 ──")]
        [SerializeField, Tooltip("再生するアニメーションの種類")]
        private AnimType _animType = AnimType.Heartbeat;

        [SerializeField, Tooltip("アニメーション全体の速度倍率（大きいほど速い）")]
        private float _speed = 1f;

        [SerializeField, Tooltip("ループの設定\n・None    = 1回だけ再生\n・Restart = 最初から繰り返す（無限）\n・Yoyo   = 往復する（無限）")]
        private LoopMode _loopMode = LoopMode.Yoyo;

        [SerializeField, Tooltip("再生開始までの遅延時間（秒）。0 = 即時再生")]
        private float _delaySeconds = 0f;

        [SerializeField, Tooltip("Start 時に自動再生するか（OFF の場合は PlayAsync() か Play() を呼ぶ）")]
        private bool _playOnAwake = true;

        // ─────────────────────────────────────────
        // Punch（パンチ）
        // ─────────────────────────────────────────
        [Header("── Punch（衝撃スケール） ──")]
        [SerializeField, Tooltip("弾ける強さ（0.3 = 元サイズの 30% 分だけ大きく弾ける）")]
        private float _punchStrength = 0.3f;

        [SerializeField, Tooltip("アニメーションの長さ（秒）")]
        private float _punchDuration = 0.4f;

        [SerializeField, Tooltip("振動回数（多いほどプルプル感が増す）")]
        private int _punchVibrato = 10;

        [SerializeField, Tooltip("弾性（0＝ピシっと止まる, 1＝バネのように戻る）"), Range(0f, 1f)]
        private float _punchElasticity = 1f;

        // ─────────────────────────────────────────
        // Shake（シェイク）
        // ─────────────────────────────────────────
        [Header("── Shake（震え） ──")]
        [SerializeField, Tooltip("震えの振れ幅（ピクセル単位）")]
        private float _shakeStrength = 20f;

        [SerializeField, Tooltip("震えアニメーションの長さ（秒）")]
        private float _shakeDuration = 0.5f;

        [SerializeField, Tooltip("振動回数（多いほど細かく震える）")]
        private int _shakeVibrato = 10;

        [SerializeField, Tooltip("ランダム度（0＝直線的, 90＝あらゆる方向に震える）")]
        private float _shakeRandomness = 90f;

        // ─────────────────────────────────────────
        // Spin（スピン）
        // ─────────────────────────────────────────
        [Header("── Spin（回転） ──")]
        [SerializeField, Tooltip("1 回転にかかる時間（秒）")]
        private float _spinDuration = 0.5f;

        // ─────────────────────────────────────────
        // Flip（フリップ）
        // ─────────────────────────────────────────
        [Header("── Flip（左右反転） ──")]
        [SerializeField, Tooltip("反転にかかる時間（秒）")]
        private float _flipDuration = 0.3f;

        [SerializeField, Tooltip("反転後の向き（ON＝左向き, OFF＝右向き）")]
        private bool _flipToLeft;

        // ─────────────────────────────────────────
        // Flash（フラッシュ）
        // ─────────────────────────────────────────
        [Header("── Flash（点滅） ──")]
        [SerializeField, Tooltip("点滅 1 回の表示⇔非表示の切り替え間隔（秒）")]
        private float _flashInterval = 0.2f;

        [SerializeField, Tooltip("点滅時の最小透明度（0＝完全透明, 1＝変化なし）"), Range(0f, 1f)]
        private float _flashMinAlpha = 0f;

        // ─────────────────────────────────────────
        // Heartbeat（ハートビート）
        // ─────────────────────────────────────────
        [Header("── Heartbeat（鼓動） ──")]
        [SerializeField, Tooltip("1 拍の最大スケール倍率（1.2 = 20% 大きくなる）")]
        private float _heartbeatScale = 1.2f;

        [SerializeField, Tooltip("1 拍（膨らみ→縮み）にかかる時間（秒）")]
        private float _heartbeatDuration = 0.4f;

        // ─────────────────────────────────────────
        // Stamp（スタンプ）
        // ─────────────────────────────────────────
        [Header("── Stamp（バウンス着地） ──")]
        [SerializeField, Tooltip("着地前の初期スケール倍率（1.5 = 1.5 倍の大きさから縮んで着地）")]
        private float _stampFromScale = 1.5f;

        [SerializeField, Tooltip("着地アニメーションの長さ（秒）")]
        private float _stampDuration = 0.6f;

        // ─────────────────────────────────────────
        // ColorCycle（カラーサイクル）
        // ─────────────────────────────────────────
        [Header("── ColorCycle（色循環） ──")]
        [SerializeField, Tooltip("変化先の色（元の色との間を往復する）")]
        private Color _colorTo = Color.yellow;

        [SerializeField, Tooltip("元の色から変化先の色まで変わる時間（秒）")]
        private float _colorCycleDuration = 0.5f;

        // ─────────────────────────────────────────
        // 内部キャッシュ
        // ─────────────────────────────────────────
        private Graphic? _graphic;           // Image / Text / RawImage を自動検出
        private SpriteRenderer? _spriteRenderer;
        private Tween? _currentTween;
        private Vector3 _defaultScale;
        private Color _defaultColor;
        private bool _initialized;

        private void Start()
        {
            Initialize();
            if (_playOnAwake) FireDirect();
        }

        private void Initialize()
        {
            if (_initialized) return;
            _graphic = GetComponent<Graphic>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _defaultScale = transform.localScale;
            _defaultColor = GetCurrentColor();
            _initialized = true;
        }

        // ─────────────────────────────────────────
        // 公開メソッド
        // ─────────────────────────────────────────

        /// <summary>Inspector で選択したアニメーションを再生する（await 可能）。</summary>
        public UniTask PlayAsync(CancellationToken ct = default)
        {
            if (!Application.isPlaying) return UniTask.CompletedTask;
            Initialize();
            return _animType switch
            {
                AnimType.Punch      => PunchAsync(ct),
                AnimType.Shake      => ShakeAsync(ct),
                AnimType.Spin       => SpinAsync(ct),
                AnimType.Flip       => FlipAsync(ct),
                AnimType.Flash      => FlashAsync(ct),
                AnimType.Heartbeat  => HeartbeatAsync(ct),
                AnimType.Stamp      => StampAsync(ct),
                AnimType.ColorCycle => ColorCycleAsync(ct),
                _                   => UniTask.CompletedTask,
            };
        }

        /// <summary>アニメーションを再生する（UnityEvent / Button.onClick に登録可能）。</summary>
        public void Play() { Initialize(); FireDirect(); }

        /// <summary>実行中のアニメーションを停止して初期状態に戻す。</summary>
        public void Stop() => KillCurrent(resetState: true);

        /// <summary>Inspector の右クリックから再生中にテストできる。</summary>
        [ContextMenu("▶ テスト再生（再生中のみ有効）")]
        private void TestPlay()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[UIAnimator] 再生中にのみ動作します"); return; }
            Initialize();
            FireDirect();
        }

        // ─────────────────────────────────────────
        // 個別アニメーション（外部から直接呼び出し可能）
        // ─────────────────────────────────────────

        public UniTask PunchAsync(CancellationToken ct = default)
        {
            Initialize(); KillCurrent();
            _currentTween = transform
                .DOPunchScale(Vector3.one * _punchStrength, _punchDuration / _speed, _punchVibrato, _punchElasticity)
                .ApplyLoop(_loopMode);
            return PlayAndAwait(ct);
        }

        public UniTask ShakeAsync(CancellationToken ct = default)
        {
            Initialize(); KillCurrent();
            _currentTween = transform
                .DOShakePosition(_shakeDuration / _speed, _shakeStrength, _shakeVibrato, _shakeRandomness)
                .ApplyLoop(_loopMode);
            return PlayAndAwait(ct);
        }

        public UniTask SpinAsync(CancellationToken ct = default)
        {
            Initialize(); KillCurrent();
            _currentTween = transform
                .DORotate(new Vector3(0f, 0f, 360f), _spinDuration / _speed, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .ApplyLoop(_loopMode);
            return PlayAndAwait(ct);
        }

        public UniTask FlipAsync(CancellationToken ct = default)
        {
            Initialize(); KillCurrent();
            float targetX = _flipToLeft ? -Mathf.Abs(_defaultScale.x) : Mathf.Abs(_defaultScale.x);
            _currentTween = transform
                .DOScaleX(targetX, _flipDuration / _speed)
                .SetEase(Ease.InOutQuad)
                .ApplyLoop(_loopMode);
            return PlayAndAwait(ct);
        }

        public async UniTask FlashAsync(CancellationToken ct = default)
        {
            Initialize(); KillCurrent();
            float half = _flashInterval / _speed * 0.5f;
            // Flash は Yoyo 固定（点滅 = 表示↔非表示の往復）。LoopMode.None なら 1往復のみ。
            int loops = _loopMode == LoopMode.None ? 2 : -1;

            if (_graphic != null)
                _currentTween = _graphic.DOFade(_flashMinAlpha, half)
                    .SetEase(Ease.Linear).SetLoops(loops, LoopType.Yoyo);
            else if (_spriteRenderer != null)
                _currentTween = _spriteRenderer.DOFade(_flashMinAlpha, half)
                    .SetEase(Ease.Linear).SetLoops(loops, LoopType.Yoyo);

            await PlayAndAwait(ct);
            SetAlpha(1f);
        }

        public UniTask HeartbeatAsync(CancellationToken ct = default)
        {
            Initialize(); KillCurrent();
            float half = _heartbeatDuration / _speed * 0.5f;
            // Heartbeat は Yoyo 固定（膨らみ↔縮みの往復）。LoopMode.None なら 1往復のみ。
            int loops = _loopMode == LoopMode.None ? 2 : -1;
            _currentTween = transform
                .DOScale(_defaultScale * _heartbeatScale, half)
                .SetEase(Ease.InOutSine)
                .SetLoops(loops, LoopType.Yoyo);
            return PlayAndAwait(ct);
        }

        public UniTask StampAsync(CancellationToken ct = default)
        {
            Initialize(); KillCurrent();
            transform.localScale = _defaultScale * _stampFromScale;
            _currentTween = transform
                .DOScale(_defaultScale, _stampDuration / _speed)
                .SetEase(Ease.OutBounce)
                .ApplyLoop(_loopMode);
            return PlayAndAwait(ct);
        }

        public async UniTask ColorCycleAsync(CancellationToken ct = default)
        {
            Initialize(); KillCurrent();
            float dur = _colorCycleDuration / _speed;
            // ColorCycle は Yoyo 固定（色Aと色Bの往復）。LoopMode.None なら 1往復のみ。
            int loops = _loopMode == LoopMode.None ? 2 : -1;

            if (_graphic != null)
                _currentTween = _graphic.DOColor(_colorTo, dur)
                    .SetEase(Ease.InOutSine).SetLoops(loops, LoopType.Yoyo);
            else if (_spriteRenderer != null)
                _currentTween = _spriteRenderer.DOColor(_colorTo, dur)
                    .SetEase(Ease.InOutSine).SetLoops(loops, LoopType.Yoyo);

            await PlayAndAwait(ct);
            SetColor(_defaultColor);
        }

        // ─────────────────────────────────────────
        // ヘルパー
        // ─────────────────────────────────────────

        /// <summary>_currentTween を Play して UniTaskCompletionSource で完了を待つ。</summary>
        private UniTask PlayAndAwait(CancellationToken ct)
        {
            if (_currentTween == null) return UniTask.CompletedTask;
            var tcs = new UniTaskCompletionSource();
            _currentTween
                .SetDelay(_delaySeconds)
                .OnComplete(() => tcs.TrySetResult())
                .OnKill(() => tcs.TrySetResult())
                .Play();
            return tcs.Task.AttachExternalCancellation(ct);
        }

        /// <summary>
        /// async を使わずに直接 DOTween を起動する（_playOnAwake / Play() / ContextMenu 用）。
        /// </summary>
        private void FireDirect()
        {
            KillCurrent();
            switch (_animType)
            {
                case AnimType.Punch:
                    _currentTween = transform
                        .DOPunchScale(Vector3.one * _punchStrength, _punchDuration / _speed, _punchVibrato, _punchElasticity)
                        .ApplyLoop(_loopMode).SetDelay(_delaySeconds).Play();
                    break;

                case AnimType.Shake:
                    _currentTween = transform
                        .DOShakePosition(_shakeDuration / _speed, _shakeStrength, _shakeVibrato, _shakeRandomness)
                        .ApplyLoop(_loopMode).SetDelay(_delaySeconds).Play();
                    break;

                case AnimType.Spin:
                    _currentTween = transform
                        .DORotate(new Vector3(0f, 0f, 360f), _spinDuration / _speed, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear).ApplyLoop(_loopMode).SetDelay(_delaySeconds).Play();
                    break;

                case AnimType.Flip:
                    float targetX = _flipToLeft ? -Mathf.Abs(_defaultScale.x) : Mathf.Abs(_defaultScale.x);
                    _currentTween = transform
                        .DOScaleX(targetX, _flipDuration / _speed)
                        .SetEase(Ease.InOutQuad).ApplyLoop(_loopMode).SetDelay(_delaySeconds).Play();
                    break;

                case AnimType.Flash:
                {
                    float half = _flashInterval / _speed * 0.5f;
                    int loops = _loopMode == LoopMode.None ? 2 : -1;
                    if (_graphic != null)
                        _currentTween = _graphic.DOFade(_flashMinAlpha, half)
                            .SetEase(Ease.Linear).SetLoops(loops, LoopType.Yoyo).SetDelay(_delaySeconds).Play();
                    else if (_spriteRenderer != null)
                        _currentTween = _spriteRenderer.DOFade(_flashMinAlpha, half)
                            .SetEase(Ease.Linear).SetLoops(loops, LoopType.Yoyo).SetDelay(_delaySeconds).Play();
                    break;
                }

                case AnimType.Heartbeat:
                {
                    float half = _heartbeatDuration / _speed * 0.5f;
                    int loops = _loopMode == LoopMode.None ? 2 : -1;
                    _currentTween = transform
                        .DOScale(_defaultScale * _heartbeatScale, half)
                        .SetEase(Ease.InOutSine).SetLoops(loops, LoopType.Yoyo).SetDelay(_delaySeconds).Play();
                    break;
                }

                case AnimType.Stamp:
                    transform.localScale = _defaultScale * _stampFromScale;
                    _currentTween = transform
                        .DOScale(_defaultScale, _stampDuration / _speed)
                        .SetEase(Ease.OutBounce).ApplyLoop(_loopMode).SetDelay(_delaySeconds).Play();
                    break;

                case AnimType.ColorCycle:
                {
                    float dur = _colorCycleDuration / _speed;
                    int loops = _loopMode == LoopMode.None ? 2 : -1;
                    if (_graphic != null)
                        _currentTween = _graphic.DOColor(_colorTo, dur)
                            .SetEase(Ease.InOutSine).SetLoops(loops, LoopType.Yoyo).SetDelay(_delaySeconds).Play();
                    else if (_spriteRenderer != null)
                        _currentTween = _spriteRenderer.DOColor(_colorTo, dur)
                            .SetEase(Ease.InOutSine).SetLoops(loops, LoopType.Yoyo).SetDelay(_delaySeconds).Play();
                    break;
                }
            }
        }

        private void KillCurrent(bool resetState = false)
        {
            _currentTween?.Kill();
            _currentTween = null;
            if (!resetState) return;
            transform.localScale = _defaultScale;
            SetColor(_defaultColor);
        }

        private Color GetCurrentColor()
        {
            if (_graphic != null) return _graphic.color;
            if (_spriteRenderer != null) return _spriteRenderer.color;
            return Color.white;
        }

        private void SetColor(Color color)
        {
            if (_graphic != null) _graphic.color = color;
            else if (_spriteRenderer != null) _spriteRenderer.color = color;
        }

        private void SetAlpha(float alpha)
        {
            var c = GetCurrentColor();
            c.a = alpha;
            SetColor(c);
        }

        private void OnDestroy() => _currentTween?.Kill();
    }

    // ─────────────────────────────────────────
    // Tween 拡張（LoopMode 適用ヘルパー）
    // ─────────────────────────────────────────
    internal static class TweenLoopExtensions
    {
        /// <summary>LoopMode に応じて SetLoops を適用する。</summary>
        internal static Tween ApplyLoop(this Tween tween, UIAnimator.LoopMode mode) => mode switch
        {
            UIAnimator.LoopMode.None    => tween,
            UIAnimator.LoopMode.Restart => tween.SetLoops(-1, LoopType.Restart),
            UIAnimator.LoopMode.Yoyo    => tween.SetLoops(-1, LoopType.Yoyo),
            _                           => tween,
        };
    }
}
