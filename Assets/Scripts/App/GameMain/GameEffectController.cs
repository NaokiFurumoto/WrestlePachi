#nullable enable
using System.Collections.Generic;
using System.Threading;
using App.Effects;
using App.Puyo;
using App.Skills;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// ゲーム中の演出を一元管理するコントローラー。
    /// ・PuyoBoard / TechSkillManager のイベントを購読して演出を再生する
    /// ・位置指定エフェクト（衝撃波など）を静的 API で提供する
    /// </summary>
    public sealed class GameEffectController : MonoBehaviour
    {
        public static GameEffectController? Instance { get; private set; }

        [Header("コンボ表示")]
        [SerializeField] private TMP_Text? _comboNumberText;
        [SerializeField] private Image?    _comboImage;
        [SerializeField] private float     _comboDisplayTime = 1.5f;

        [Header("エフェクト配置先")]
        [SerializeField] private Transform? _effectRoot;

        [Header("スキルエフェクト Prefab")]
        [SerializeField] private ArrowSweepEffect?    _arrowSweepPrefab;
        [SerializeField] private VerticalSweepEffect? _verticalSweepPrefab;
        [SerializeField] private PuyoBurstEffect?     _puyoBurstPrefab;
        [SerializeField] private StompEffect?         _stompPrefab;
        [SerializeField] private LariatEffect?        _lariatPrefab;
        [SerializeField] private TackleEffect?        _tacklePrefab;
        [SerializeField] private OjamaDissolveEffect? _ojamadissolvePrefab;

        private CancellationTokenSource? _comboCts;
        private PuyoBoard?               _board;

        // ── ライフサイクル ────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            _comboCts?.Cancel();
            if (Instance == this) Instance = null;
        }

        // ── 初期化 ────────────────────────────────────────────────────

        public void Initialize(PuyoBoard board, TechSkillManager skillManager)
        {
            _board                        = board;
            board.OnAboutToClear         += OnAboutToClear;
            board.OnChainStep            += OnChainStep;
            skillManager.OnSkillExecuted += OnSkillExecuted;
        }

        public void Dispose(PuyoBoard board, TechSkillManager skillManager)
        {
            _board                        = null;
            board.OnAboutToClear         -= OnAboutToClear;
            board.OnChainStep            -= OnChainStep;
            skillManager.OnSkillExecuted -= OnSkillExecuted;
        }

        // ── スキルエフェクト API ──────────────────────────────────────

        /// <summary>
        /// 指定ワールド座標に矢印スイープエフェクトを発生させる（Fire and Forget）。
        /// </summary>
        public void PlayArrowSweep(Vector3 worldPos, CancellationToken ct)
        {
            if (_arrowSweepPrefab == null) return;
            Instantiate(_arrowSweepPrefab, worldPos, Quaternion.identity, _effectRoot)
                .PlayAsync(ct).Forget();
        }

        /// <summary>
        /// 指定ワールド座標から縦方向スイープエフェクトを発生させる（Fire and Forget）。
        /// </summary>
        public void PlayVerticalSweep(Vector3 worldPos, CancellationToken ct)
        {
            if (_verticalSweepPrefab == null) return;
            Instantiate(_verticalSweepPrefab, worldPos, Quaternion.identity, _effectRoot)
                .PlayAsync(ct).Forget();
        }

        /// <summary>
        /// 指定ワールド座標にラリアットアニメーションエフェクトを発生させる（Fire and Forget）。
        /// </summary>
        public void PlayLariat(Vector3 worldPos, CancellationToken ct)
        {
            if (_lariatPrefab == null) return;
            Instantiate(_lariatPrefab, worldPos, Quaternion.identity, _effectRoot)
                .PlayAsync(ct).Forget();
        }

        /// <summary>
        /// 指定ワールド座標にスタンプ落下エフェクトを発生させる（Fire and Forget）。
        /// </summary>
        public void PlayStomp(Vector3 worldPos, CancellationToken ct)
        {
            if (_stompPrefab == null) return;
            Instantiate(_stompPrefab, worldPos, Quaternion.identity, _effectRoot)
                .PlayAsync(ct).Forget();
        }

        /// <summary>
        /// 指定ワールド座標にぷよ爆散パーティクルを発生させる（Fire and Forget）。
        /// </summary>
        public void PlayBurst(Vector3 worldPos, PuyoColor color, CancellationToken ct)
        {
            if (_puyoBurstPrefab == null) return;
            Instantiate(_puyoBurstPrefab, worldPos, Quaternion.identity, _effectRoot)
                .PlayAsync(color, ct).Forget();
        }

        public UniTask PlayTackleAsync(Vector3 spawnPos, Vector3 targetPos, CancellationToken ct)
        {
            if (_tacklePrefab == null) return UniTask.CompletedTask;
            return Instantiate(_tacklePrefab, spawnPos, Quaternion.identity, _effectRoot)
                .PlayAsync(targetPos, ct);
        }

        public UniTask PlayOjamaDissolveAsync(Vector3 worldPos, Sprite sprite, CancellationToken ct)
        {
            if (_ojamadissolvePrefab == null) return UniTask.CompletedTask;
            return Instantiate(_ojamadissolvePrefab, worldPos, Quaternion.identity, _effectRoot)
                .PlayAsync(sprite, ct);
        }

        // ── ぷよが消える直前 ────────────────────────────────────────

        private void OnAboutToClear(IReadOnlyList<Vector2Int> positions)
        {
            if (_board == null) return;
            foreach (var cell in positions)
            {
                var worldPos = _board.CellToWorld(cell);
                var color    = _board.GetColorAt(cell) ?? PuyoColor.RED;
                PlayBurst(worldPos, color, destroyCancellationToken);
            }
        }

        // ── 連鎖ステップ完了 ─────────────────────────────────────────

        private void OnChainStep(int step, int clearedCount)
        {
            ShowComboAsync(step, destroyCancellationToken).Forget();
        }

        private async UniTaskVoid ShowComboAsync(int step, CancellationToken ct)
        {
            _comboCts?.Cancel();
            _comboCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var show = step >= 2;

            if (_comboNumberText != null)
            {
                _comboNumberText.text    = show ? step.ToString() : string.Empty;
                _comboNumberText.enabled = show;
            }

            if (_comboImage != null)
                _comboImage.enabled = show;

            if (show)
            {
                await UniTask.Delay(
                    (int)(_comboDisplayTime * 1000),
                    cancellationToken: _comboCts.Token
                ).SuppressCancellationThrow();

                if (_comboNumberText != null) _comboNumberText.enabled = false;
                if (_comboImage      != null) _comboImage.enabled      = false;
            }
        }

        // ── スキル発動 ───────────────────────────────────────────────

        private void OnSkillExecuted(HoldType holdType) { }

    }
}
#nullable disable
