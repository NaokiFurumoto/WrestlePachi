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

        /// <summary>予告を続けて出すときの間隔（秒）。ステージ開始時に技がいくつも付いても重ならないよう順番に出す</summary>
        private const float SkillWarningSpacing = 0.6f;
        private static readonly Color AngryFaceColor = new(1f, 0.35f, 0.35f);
        private float _nextSkillWarningTime;

        /// <summary>
        /// 敵スキルの予告：敵の顔が赤く光って揺れ、顔の右横に技アイコンを大きくポンと出し、その横に技名の帯を出す。
        /// 同時に画面の縁を赤黒く脈打たせる（ScreenEffectController.PlayEnemyWarning）。
        /// 前の予告が出ている間に次が来たら、少し待ってから出す。Prefab を使わず実行時に作る。
        /// </summary>
        public void ShowSkillWarning(Sprite? icon, string? skillName)
        {
            if (_enemyFaceImage == null) return;

            var delay = Mathf.Max(0f, _nextSkillWarningTime - Time.time);
            _nextSkillWarningTime = Time.time + delay + SkillWarningSpacing;
            if (delay <= 0f) PlaySkillWarning(icon, skillName);
            else DOVirtual.DelayedCall(delay, () => PlaySkillWarning(icon, skillName)).SetLink(gameObject);
        }

        private void PlaySkillWarning(Sprite? icon, string? skillName)
        {
            if (_enemyFaceImage == null) return;
            ScreenEffectController.PlayEnemyWarning();
            PulseEdgeGlow();

            var face     = _enemyFaceImage.rectTransform;
            var faceRect = face.rect;
            var size     = faceRect.height * 1.05f;
            var iconPos  = face.localPosition + new Vector3(faceRect.xMax + 10f + size * 0.5f, faceRect.center.y, 0f);

            // 敵の顔：赤く2回光り、怒ってぶるっと揺れる
            _enemyFaceImage.DOKill(complete: true);
            face.DOKill(complete: true);
            DOTween.Sequence()
                .Append(_enemyFaceImage.DOColor(AngryFaceColor, 0.08f))
                .Append(_enemyFaceImage.DOColor(Color.white, 0.16f))
                .Append(_enemyFaceImage.DOColor(AngryFaceColor, 0.08f))
                .Append(_enemyFaceImage.DOColor(Color.white, 0.3f))
                .SetTarget(_enemyFaceImage)
                .SetLink(_enemyFaceImage.gameObject);
            face.DOPunchRotation(new Vector3(0f, 0f, 14f), 0.45f, 12, 0.6f).SetLink(face.gameObject);

            // 技名の帯（アイコンの右横。アイコンより先に作って、アイコンの後ろに置く）
            if (!string.IsNullOrEmpty(skillName))
                ShowSkillNameBanner(face.parent, iconPos + new Vector3(size * 0.35f, 0f, 0f), size, skillName!);

            if (icon == null) return;
            var go = new GameObject("EnemySkillWarning", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(face.parent, false);
            rt.SetAsLastSibling();
            rt.sizeDelta     = new Vector2(size, size);
            rt.localPosition = iconPos;
            rt.localScale    = Vector3.zero;

            var image = go.GetComponent<Image>();
            image.sprite        = icon;
            image.raycastTarget = false;

            DOTween.Sequence()
                .Append(rt.DOScale(1.35f, 0.15f).SetEase(Ease.OutBack))
                .Append(rt.DOScale(1f, 0.1f).SetEase(Ease.OutQuad))
                .Join(rt.DOPunchRotation(new Vector3(0f, 0f, 18f), 0.35f, 8, 0.5f))
                .AppendInterval(0.55f)
                .Append(rt.DOScale(0f, 0.2f).SetEase(Ease.InBack))
                .Join(image.DOFade(0f, 0.2f))
                .OnComplete(() => Destroy(go))
                .SetLink(go);
        }

        /// <summary>技名：「サメ！」の文字をアイコンの右横に、左から伸ばして出し、少し止まってから縮めて消す</summary>
        private void ShowSkillNameBanner(Transform parent, Vector3 leftCenter, float height, string skillName)
        {
            if (_enemyHpText == null) return;

            var holder = new GameObject("EnemySkillName", typeof(RectTransform));
            var hrt    = (RectTransform)holder.transform;
            hrt.SetParent(parent, false);
            hrt.SetAsLastSibling();
            hrt.pivot         = new Vector2(0f, 0.5f);
            hrt.sizeDelta     = new Vector2(height * 6f, height * 0.7f);
            hrt.localPosition = leftCenter;
            hrt.localScale    = new Vector3(0f, 1f, 1f);

            // 文字は HP の数字の見た目（フォント・縁取り・色）を借りて作る
            var label = Instantiate(_enemyHpText, hrt);
            label.name             = "Label";
            label.text             = $"{skillName}！";
            label.raycastTarget    = false;
            label.enableAutoSizing = false;
            label.alignment        = TextAlignmentOptions.MidlineLeft;
            label.fontSize         = height * 0.42f;
            var lrt = label.rectTransform;
            lrt.anchorMin  = Vector2.zero;
            lrt.anchorMax  = Vector2.one;
            lrt.offsetMin  = new Vector2(height * 0.55f, 0f); // アイコンの右半分に隠れない位置から
            lrt.offsetMax  = Vector2.zero;
            lrt.pivot      = new Vector2(0f, 0.5f);
            lrt.localScale = Vector3.one;

            DOTween.Sequence()
                .Append(hrt.DOScaleX(1f, 0.18f).SetEase(Ease.OutBack))
                .AppendInterval(0.7f)
                .Append(hrt.DOScaleY(0f, 0.15f).SetEase(Ease.InQuad))
                .OnComplete(() => Destroy(holder))
                .SetLink(holder);
        }

        // ── 敵スキルの予告：画面の縁を赤く光らせる ─────────────────────

        private Image? _edgeGlow;
        private Tween? _edgeGlowTween;
        private static Sprite? _edgeGlowSprite;
        private static readonly Color EdgeGlowColor = new(1f, 0.12f, 0.08f);

        /// <summary>
        /// 縁の光の濃さ（uv は左下0・右上1）。中央は0、上下左右の端へ近づくほど濃くなり、角がいちばん濃い。
        /// 盤面のある中央はふさがない。
        /// </summary>
        public static float EdgeGlowAlpha(float u, float v)
        {
            var sx = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 1f, Mathf.Abs(u * 2f - 1f)));
            var sy = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.88f, 1f, Mathf.Abs(v * 2f - 1f)));
            return 1f - (1f - sx) * (1f - sy);
        }

        /// <summary>画面の縁を赤く「ドクン、ドクン」と2回光らせる（暗い背景の上でも目のはしで気づける）</summary>
        private void PulseEdgeGlow()
        {
            var glow = EnsureEdgeGlow();
            if (glow == null) return;
            glow.transform.SetAsLastSibling();
            _edgeGlowTween?.Kill();
            glow.color = new Color(EdgeGlowColor.r, EdgeGlowColor.g, EdgeGlowColor.b, 0f);
            _edgeGlowTween = DOTween.Sequence()
                .Append(glow.DOFade(0.7f,  0.08f).SetEase(Ease.OutQuad))
                .Append(glow.DOFade(0.2f,  0.16f).SetEase(Ease.InQuad))
                .Append(glow.DOFade(0.7f,  0.08f).SetEase(Ease.OutQuad))
                .Append(glow.DOFade(0f,    0.5f).SetEase(Ease.InOutSine))
                .SetLink(glow.gameObject);
        }

        private Image? EnsureEdgeGlow()
        {
            if (_edgeGlow != null) return _edgeGlow;
            var canvas = FindHudCanvas();
            if (canvas == null) return null;

            var go = new GameObject("EnemyWarningEdgeGlow", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _edgeGlow = go.GetComponent<Image>();
            _edgeGlow.sprite        = _edgeGlowSprite ??= CreateEdgeGlowSprite();
            _edgeGlow.raycastTarget = false;
            _edgeGlow.color         = new Color(EdgeGlowColor.r, EdgeGlowColor.g, EdgeGlowColor.b, 0f);
            return _edgeGlow;
        }

        /// <summary>演出を載せる HUD のキャンバス（View の子にあるので、親になければ子から探す）</summary>
        private Canvas? FindHudCanvas()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = GetComponentInChildren<Canvas>();
            return canvas;
        }

        private static Sprite CreateEdgeGlowSprite()
        {
            const int w = 64, h = 112;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px  = new Color32[w * h];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var a = EdgeGlowAlpha((x + 0.5f) / w, (y + 0.5f) / h);
                px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
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

        // ── 敵スキル「黒保留威力」：パンパン ────────────────────────────

        /// <summary>
        /// 黒保留の消化時：画面の上寄りに黒い保留玉が出て、横から黒い小悪魔が筆でシュコシュコあおるたびに玉が膨らみ、
        /// 「お邪魔 2個」の数字が跳ね上がって、最後に破裂する。破裂した瞬間に完了する（残りの演出は裏で続く）。
        /// </summary>
        public async UniTask PlayBlackPowerUpAsync(Sprite? ball, Sprite? devil, int from, int to, System.Threading.CancellationToken ct)
        {
            var canvas = FindHudCanvas();
            if (canvas == null || _enemyHpText == null) return;
            var canvasRt = (RectTransform)canvas.transform;
            var size     = Mathf.Min(canvasRt.rect.width, canvasRt.rect.height) * 0.2f;
            var center   = new Vector3(size * 0.25f, canvasRt.rect.height * 0.1f, 0f);

            var root = new GameObject("BlackPowerUp", typeof(RectTransform));
            var rrt  = (RectTransform)root.transform;
            rrt.SetParent(canvasRt, false);
            rrt.SetAsLastSibling();
            rrt.localPosition = center;

            // 黒い保留玉
            var ballGo = new GameObject("Ball", typeof(RectTransform), typeof(Image));
            var brt    = (RectTransform)ballGo.transform;
            brt.SetParent(rrt, false);
            brt.sizeDelta = new Vector2(size, size);
            var ballImg = ballGo.GetComponent<Image>();
            ballImg.sprite = ball; ballImg.preserveAspect = true; ballImg.raycastTarget = false;
            if (ball == null) ballImg.color = new Color(0.1f, 0.05f, 0.15f);
            brt.localScale = Vector3.zero;

            // 小悪魔（玉の左。筆先が玉にかかる）
            var devilGo = new GameObject("Devil", typeof(RectTransform), typeof(Image));
            var drt     = (RectTransform)devilGo.transform;
            drt.SetParent(rrt, false);
            var aspect = devil != null ? devil.rect.width / devil.rect.height : 1f;
            drt.sizeDelta = new Vector2(size * 1.3f, size * 1.3f / aspect);
            var devilImg = devilGo.GetComponent<Image>();
            devilImg.sprite = devil; devilImg.preserveAspect = true; devilImg.raycastTarget = false;
            if (devil == null) devilImg.color = new Color(0.18f, 0.1f, 0.25f);
            var devilRest = new Vector3(-size * 0.95f, size * 0.25f, 0f);
            drt.localPosition = devilRest + new Vector3(-size * 3f, size * 1.5f, 0f);

            // 数字（HP の数字の見た目を借りる）
            var label = Instantiate(_enemyHpText, rrt);
            label.name             = "Count";
            label.raycastTarget    = false;
            label.enableAutoSizing = false;
            label.alignment        = TextAlignmentOptions.Center;
            label.fontSize         = size * 0.32f;
            label.text             = $"お邪魔 {from}個";
            var lrt = label.rectTransform;
            lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta     = new Vector2(size * 3f, size * 0.5f);
            lrt.localPosition = new Vector3(0f, -size * 0.85f, 0f);
            lrt.localScale    = Vector3.one;

            try
            {
                brt.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetLink(ballGo);
                drt.DOLocalJump(devilRest, size * 0.4f, 1, 0.3f).SetLink(devilGo);
                await UniTask.Delay(320, cancellationToken: ct);

                // シュコ×3：あおるたびに膨らんで数字が増える
                const int pumps = 3;
                for (var k = 1; k <= pumps; k++)
                {
                    DOTween.Sequence()
                        .Append(drt.DOLocalRotate(new Vector3(0f, 0f, -22f), 0.08f))
                        .Append(drt.DOLocalRotate(Vector3.zero, 0.12f))
                        .SetLink(devilGo);
                    await UniTask.Delay(80, cancellationToken: ct);
                    var shown = from + Mathf.RoundToInt((to - from) * k / (float)pumps);
                    label.text = $"お邪魔 {shown}個";
                    lrt.DOKill(complete: true);
                    lrt.DOPunchScale(Vector3.one * 0.35f, 0.2f, 6, 0.5f).SetLink(label.gameObject);
                    brt.DOScale(1f + 0.28f * k, 0.16f).SetEase(Ease.OutBack).SetLink(ballGo);
                    await UniTask.Delay(260, cancellationToken: ct);
                }

                // 破裂：ふくらみきって弾け、画面が揺れる
                brt.DOKill();
                brt.DOScale(2.6f, 0.12f).SetEase(Ease.OutQuad).SetLink(ballGo);
                ballImg.DOFade(0f, 0.12f).SetLink(ballGo);
                ScreenShakeController.Shake(ShakePreset.Medium);
                label.color = new Color(1f, 0.35f, 0.35f);
                lrt.DOScale(1.5f, 0.15f).SetEase(Ease.OutBack).SetLink(label.gameObject);
            }
            catch (System.OperationCanceledException)
            {
                Destroy(root);
                throw;
            }

            // 小悪魔は笑って去り、数字は少し残ってから消える（裏で続ける）
            DOTween.Sequence()
                .Append(drt.DOPunchPosition(new Vector3(0f, size * 0.15f, 0f), 0.4f, 8, 0.6f))
                .Append(drt.DOLocalMove(devilRest + new Vector3(size * 4f, size * 2.5f, 0f), 0.4f).SetEase(Ease.InQuad))
                .Join(devilImg.DOFade(0f, 0.4f))
                .Insert(0.5f, label.DOFade(0f, 0.3f))
                .OnComplete(() => Destroy(root))
                .SetLink(root);
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
