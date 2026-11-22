#nullable enable
using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameSys;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
        // ── オプションボタン ─────────────────────────────────────────────
        public event Action? OptionClicked;
        public void OnOptionButtonClicked() => OptionClicked?.Invoke();

        // ── 敵HPゲージ・顔 ──────────────────────────────────────────────
        [Header("敵HPゲージ・顔")]
        [SerializeField] private Image            _hpBarImage  = default!;
        [SerializeField] private Image?           _enemyFaceImage;
        [SerializeField] private TMP_Text?        _enemyHpText;
        [SerializeField] private float            _hpAnimDuration = 0.4f;
        [SerializeField] private DamageEffect?    _damageEffect;
        [SerializeField] private UIAnimator?      _enemyAnimator;


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

        // ── 敵レベル ─────────────────────────────────────────────────
        [Header("敵レベル")]
        [FormerlySerializedAs("_roundText")]
        [SerializeField] private TMP_Text? _levelText;

        // ── 虹保留確率 ───────────────────────────────────────────────
        [Header("虹保留確率")]
        [SerializeField] private TMP_Text? _rainbowProbText;

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
                _enemy.OnHealed    -= ShowHeal;
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
                _enemy.OnHealed    -= ShowHeal;
            }
            _enemy = enemy;
            _enemy.OnHpChanged += OnEnemyHpChanged;
            _enemy.OnEnemySet  += SetEnemyFace;
            _enemy.OnDamaged   += OnEnemyDamaged;
            _enemy.OnHealed    += ShowHeal;

            float fill = _enemy.MaxHp > 0
                ? (float)_enemy.CurrentHp / _enemy.MaxHp : 1f;
            SetHpFillImmediate(fill);
            SetEnemyFace(_enemy.FaceSprite);
            SetEnemyHpText(_enemy.CurrentHp);
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
            SetEnemyHpText(currentHp);
        }

        private void OnEnemyDamaged(int damage)
        {
if (_damageEffect != null)
                _damageEffect.PlayAsync(damage, destroyCancellationToken).Forget();
            if (_enemyAnimator != null)
                _enemyAnimator.ShakeAsync(destroyCancellationToken).Forget();
        }

        private void SetEnemyHpText(int currentHp)
        {
            if (_enemyHpText == null) return;
            _enemyHpText.text = currentHp.ToString();
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

        // ── 敵スキルの予告・回復表示 ─────────────────────────────────

        /// <summary>
        /// 敵スキルの予告：敵の顔の右横で技アイコンをポンと弾ませて出し、少し止めてから縮めて消す。
        /// Prefab を使わず実行時に Image を生成する。
        /// </summary>
        public void ShowSkillWarning(Sprite? icon)
        {
            if (icon == null || _enemyFaceImage == null) return;

            var face     = _enemyFaceImage.rectTransform;
            var faceRect = face.rect;
            var size     = faceRect.height * 0.8f;

            var go = new GameObject("EnemySkillWarning", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(face.parent, false);
            rt.SetAsLastSibling();
            rt.sizeDelta     = new Vector2(size, size);
            rt.localPosition = face.localPosition
                             + new Vector3(faceRect.xMax + 8f + size * 0.5f, faceRect.center.y, 0f);
            rt.localScale    = Vector3.zero;

            var image = go.GetComponent<Image>();
            image.sprite        = icon;
            image.raycastTarget = false;

            DOTween.Sequence()
                .Append(rt.DOScale(1.3f, 0.15f).SetEase(Ease.OutBack))
                .Append(rt.DOScale(1f, 0.1f).SetEase(Ease.OutQuad))
                .AppendInterval(0.35f)
                .Append(rt.DOScale(0f, 0.2f).SetEase(Ease.InBack))
                .Join(image.DOFade(0f, 0.2f))
                .OnComplete(() => Destroy(go))
                .SetLink(go);
        }

        /// <summary>
        /// 敵の回復表示：HP の数字を複製して緑の「+N」にし、浮かび上がらせながら消す。
        /// </summary>
        public void ShowHeal(int amount)
        {
            if (_enemyHpText == null || amount <= 0) return;

            var popup = Instantiate(_enemyHpText, _enemyHpText.transform.parent);
            popup.name                 = "EnemyHealPopup";
            popup.text                 = $"+{amount}";
            popup.raycastTarget        = false;
            popup.enableVertexGradient = false;
            popup.color                = Color.white;
            // HP の数字はフォントのマテリアル側で色が付いているので、複製側だけ明るい緑に塗り替える
            popup.fontMaterial.SetColor(ShaderUtilities.ID_FaceColor, new Color(0.45f, 1f, 0.45f));

            // HP の数字に重ならないよう少し上から出し、ポンと弾んでから浮かび上がって消える
            var rt    = popup.rectTransform;
            var start = rt.anchoredPosition + new Vector2(0f, 45f);
            rt.anchoredPosition = start;
            rt.localScale       = Vector3.one * 0.5f;
            DOTween.Sequence()
                .Append(rt.DOScale(1.9f, 0.12f).SetEase(Ease.OutBack))
                .Append(rt.DOScale(1.5f, 0.08f))
                .Join(rt.DOAnchorPos(start + new Vector2(0f, 70f), 0.8f).SetEase(Ease.OutCubic))
                .Insert(0.55f, popup.DOFade(0f, 0.3f))
                .OnComplete(() => Destroy(popup.gameObject))
                .SetLink(popup.gameObject);
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

        /// <summary>
        /// 敵スキル「時計」：ネズミがタイマーを bites 回かじる演出（ClockMouseEffect）。onBite はかじるたびに呼ぶ。
        /// </summary>
        public UniTask PlayClockGnawAsync(Sprite? mouse, int bites, Action onBite, System.Threading.CancellationToken ct)
        {
            if (_timerText == null)
            {
                for (var i = 0; i < bites; i++) onBite();
                return UniTask.CompletedTask;
            }
            return ClockMouseEffect.PlayAsync(_timerText, mouse, bites, onBite, ct);
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

        // ── 敵レベル ─────────────────────────────────────────────────

        /// <summary>敵レベルを常に「Lv.N」形式で表示する（同じ敵を複数ステージで使い回す想定）</summary>
        public void SetEnemyLevel(int level)
        {
            if (_levelText == null) return;
            _levelText.text = $"{level}";
        }

        // ── 虹保留確率 ───────────────────────────────────────────────

        public void SetRainbowProbability(int denominator)
        {
            if (_rainbowProbText == null) return;
            _rainbowProbText.text = denominator > 0 ? $"1/{denominator}" : "なし";
        }

    }
}
#nullable disable
