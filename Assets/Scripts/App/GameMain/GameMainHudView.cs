#nullable enable
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// ゲームメイン画面の HUD 全体を管理する View。
    /// 敵HPゲージ・制限時間・スタミナ・ステージ数・オプションボタン等をまとめる基底クラス。
    /// </summary>
    public sealed class GameMainHudView : MonoBehaviour
    {
        // ── 敵HPゲージ ───────────────────────────────────────────────
        [Header("敵HPゲージ")]
        [SerializeField] private Image            _hpBarImage  = default!;
        [SerializeField] private EnemyController? _enemy;
        [SerializeField] private float            _hpAnimDuration = 0.4f;

        private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");

        private Material _hpMaterial = default!;
        private float    _currentFill;
        private Tweener? _hpTween;

        // ── TODO: 制限時間 ───────────────────────────────────────────
        // [Header("制限時間")]
        // [SerializeField] private TimerGage _timerGage = default!;

        // ── TODO: スタミナ ───────────────────────────────────────────
        // [Header("スタミナ")]
        // [SerializeField] private Image _staminaBar = default!;

        // ── TODO: ステージ数 ─────────────────────────────────────────
        // [Header("ステージ")]
        // [SerializeField] private TMP_Text _stageLabel = default!;

        // ── TODO: オプションボタン ───────────────────────────────────
        // [Header("オプション")]
        // [SerializeField] private Button _optionButton = default!;

        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            // マテリアルをインスタンス化して専有（共有マテリアルを汚染しない）
            _hpMaterial = Instantiate(_hpBarImage.material);
            _hpBarImage.material = _hpMaterial;
        }

        private void Start()
        {
            if (_enemy == null) return;

            _enemy.OnHpChanged += OnEnemyHpChanged;

            // 初期値を即反映（アニメーションなし）
            float initialFill = _enemy.MaxHp > 0
                ? (float)_enemy.CurrentHp / _enemy.MaxHp
                : 1f;
            SetHpFillImmediate(initialFill);
        }

        private void OnDestroy()
        {
            if (_enemy != null) _enemy.OnHpChanged -= OnEnemyHpChanged;
            _hpTween?.Kill();
            if (_hpMaterial != null) Destroy(_hpMaterial);
        }

        // ── 敵HP ─────────────────────────────────────────────────────

        /// <summary>
        /// EnemyController を外部からセットし購読を切り替える。
        /// 次の敵に切り替えるとき等に GameMainController から呼ぶ。
        /// </summary>
        public void SetEnemy(EnemyController enemy)
        {
            if (_enemy != null) _enemy.OnHpChanged -= OnEnemyHpChanged;
            _enemy = enemy;
            _enemy.OnHpChanged += OnEnemyHpChanged;

            float fill = _enemy.MaxHp > 0
                ? (float)_enemy.CurrentHp / _enemy.MaxHp
                : 1f;
            SetHpFillImmediate(fill);
        }

        /// <summary>即座にゲージをセット（アニメーションなし。敵切り替え時など）</summary>
        public void SetHpFillImmediate(float fill)
        {
            _hpTween?.Kill();
            _currentFill = Mathf.Clamp01(fill);
            _hpMaterial.SetFloat(FillAmountId, _currentFill);
        }

        // EnemyController.OnHpChanged に接続
        private void OnEnemyHpChanged(int currentHp, int maxHp)
        {
            float target = maxHp > 0 ? (float)currentHp / maxHp : 0f;
            AnimateHpFill(target);
        }

        private void AnimateHpFill(float target)
        {
            _hpTween?.Kill();
            _hpTween = DOTween
                .To(
                    () => _currentFill,
                    v  => { _currentFill = v; _hpMaterial.SetFloat(FillAmountId, v); },
                    Mathf.Clamp01(target),
                    _hpAnimDuration
                )
                .SetEase(Ease.OutCubic);
        }
    }
}
#nullable disable
