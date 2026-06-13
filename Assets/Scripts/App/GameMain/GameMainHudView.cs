#nullable enable
using DG.Tweening;
using GameSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// ゲームメイン画面の HUD 全体を管理する View。
    /// タイマー・ステージ数・スタミナ・敵HP・敵顔画像の表示を担う純粋な表示コンポーネント。
    /// GameMainController から SetEnemy / SetTime / SetStage を呼んで反映する。
    /// </summary>
    public sealed class GameMainHudView : ViewBase
    {
        // ── 敵HPゲージ・顔 ──────────────────────────────────────────────
        [Header("敵HPゲージ・顔")]
        [SerializeField] private Image            _hpBarImage  = default!;
        [SerializeField] private Image?           _enemyFaceImage;
        [SerializeField] private TMP_Text?        _enemyHpText;
        [SerializeField] private float            _hpAnimDuration = 0.4f;
        [SerializeField] private DamageEffect?    _damageEffect;

        private EnemyController? _enemy;

        private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");

        private Material _hpMaterial = default!;
        private float    _currentFill;
        private Tweener? _hpTween;

        // ── 制限時間 ─────────────────────────────────────────────────
        [Header("制限時間")]
        [SerializeField] private TMP_Text? _timerText;

        // ── ステージ数 ───────────────────────────────────────────────
        [Header("ステージ数")]
        [SerializeField] private TMP_Text? _stageText;

        // ── スタミナ ─────────────────────────────────────────────────
        [Header("スタミナ")]
        [SerializeField] private TMP_Text? _staminaText;

        // ─────────────────────────────────────────────────────────────

        protected override void OnInitialize()
        {
            // 敵HPゲージ・顔画像初期化
            if (_hpBarImage != null)
            {
                _hpMaterial = Instantiate(_hpBarImage.material);
                _hpBarImage.material = _hpMaterial;
            }

            if (StaminaManager.isValid)
            {
                StaminaManager.Instance.OnChanged += SetStamina;
                SetStamina(StaminaManager.Instance.Current, StaminaManager.Instance.Max);
            }
        }

        protected override void OnRelease()
        {
            if (_enemy != null)
            {
                _enemy.OnHpChanged -= OnEnemyHpChanged;
                _enemy.OnEnemySet  -= SetEnemyFace;
                _enemy.OnDamaged   -= OnEnemyDamaged;
            }
            _hpTween?.Kill();
            if (_hpMaterial != null) Destroy(_hpMaterial);
            if (StaminaManager.isValid) StaminaManager.Instance.OnChanged -= SetStamina;
        }

        // ── 敵HP ─────────────────────────────────────────────────────

        public void SetEnemy(EnemyController enemy)
        {
            if (_enemy != null)
            {
                _enemy.OnHpChanged -= OnEnemyHpChanged;
                _enemy.OnEnemySet  -= SetEnemyFace;
                _enemy.OnDamaged   -= OnEnemyDamaged;
            }
            _enemy = enemy;
            _enemy.OnHpChanged += OnEnemyHpChanged;
            _enemy.OnEnemySet  += SetEnemyFace;
            _enemy.OnDamaged   += OnEnemyDamaged;

            float fill = _enemy.MaxHp > 0
                ? (float)_enemy.CurrentHp / _enemy.MaxHp : 1f;
            SetHpFillImmediate(fill);
            SetEnemyFace(_enemy.FaceSprite);
            SetEnemyHpText(_enemy.CurrentHp, _enemy.MaxHp);
        }

        private void SetHpFillImmediate(float fill)
        {
            _hpTween?.Kill();
            _currentFill = Mathf.Clamp01(fill);
            if (_hpMaterial != null) _hpMaterial.SetFloat(FillAmountId, _currentFill);
        }

        private void OnEnemyHpChanged(int currentHp, int maxHp)
        {
            float target = maxHp > 0 ? (float)currentHp / maxHp : 0f;
            AnimateHpFill(target);
            SetEnemyHpText(currentHp, maxHp);
        }

        private void OnEnemyDamaged(int damage)
        {
            _damageEffect?.PlayAsync(damage, destroyCancellationToken).Forget();
        }

        private void SetEnemyHpText(int currentHp, int maxHp)
        {
            if (_enemyHpText == null) return;
            _enemyHpText.text = $"{currentHp}/{maxHp}";
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

        // ── 敵顔画像 ─────────────────────────────────────────────────

        public void SetEnemyFace(Sprite? sprite)
        {
            if (_enemyFaceImage == null) return;
            _enemyFaceImage.sprite  = sprite;
            _enemyFaceImage.enabled = sprite != null;
        }

        // ── タイマー ─────────────────────────────────────────────────

        public void SetTime(float seconds)
        {
            if (_timerText == null) return;
            var sec = Mathf.CeilToInt(seconds);
            _timerText.text = $"{sec / 60:00}:{sec % 60:00}";
        }

        // ── ステージ数 ───────────────────────────────────────────────

        public void SetStage(int current, int total)
        {
            if (_stageText == null) return;
            _stageText.text = $"{current}/{total}";
        }

        // ── スタミナ ─────────────────────────────────────────────────

        public void SetStamina(int current, int max)
        {
            if (_staminaText == null) return;
            _staminaText.text = $"{current}/{max}";
        }
    }
}
#nullable disable
