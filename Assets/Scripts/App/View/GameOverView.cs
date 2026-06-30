#nullable enable
using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// ゲームオーバー演出 View。
    /// 演出順：GAME OVER画像落下 → 背景グレースケール化 → 情報グループフェードイン → ボタン表示
    /// Prefab: Resources/Prefabs/Views/GameOverView
    /// </summary>
    public sealed class GameOverView : ViewBase
    {
        // ─── ViewData ────────────────────────────────────────────
        public sealed class GameOverViewData : ViewData
        {
            public int     StageNumber    { get; set; }
            public string  EnemyName      { get; set; } = "";
            public Sprite? EnemySprite    { get; set; }
            public int     CurrentStamina { get; set; }
            public int     MaxStamina     { get; set; }
            public Action? OnRetry        { get; set; }
            public Action? OnTitle        { get; set; }
        }

        // ─── Inspector フィールド ─────────────────────────────────
        [Header("GAME OVER 画像（上から落下）")]
        [SerializeField] private RectTransform? _gameOverImageRect;
        [SerializeField] private UIAnimator?    _gameOverBobAnimator;  // 落下後のBobループ

        [Header("背景（グレースケールマテリアル適用）")]
        [SerializeField] private Image? _backgroundImage;

        [Header("情報グループ（alpha 0→1 でフェードイン）")]
        [SerializeField] private CanvasGroup? _infoGroup;

        [Header("敵顔")]
        [SerializeField] private Image? _enemyImage;

        [Header("テキスト")]
        [SerializeField] private TMP_Text? _stageNumberText;
        [SerializeField] private TMP_Text? _enemyNameText;

        [Header("スタミナ")]
        [SerializeField] private TMP_Text? _staminaText;
        [SerializeField] private TMP_Text? _recoveryTimerText;

        [Header("ボタン")]
        [SerializeField] private GameObject?            _btnRoot;
        [SerializeField] private UnityEngine.UI.Button? _btnRetry;
        [SerializeField] private UnityEngine.UI.Button? _btnStaminaRecover;

        [Header("タイミング（秒）")]
        [Tooltip("GAME OVER画像が落下を開始する画面上部へのオフセット量（px）。大きいほど遠くから落ちてくる")]
        [SerializeField] private float _dropStartOffsetY  = 400f;

        [Tooltip("GAME OVER画像が定位置まで落下するのにかかる時間（秒）。大きいほどゆっくり落ちる")]
        [SerializeField] private float _dropDuration      = 0.7f;

        [Tooltip("GAME OVER画像が到着してからグレースケールが始まるまでの待機時間（秒）。間を置きたいときに増やす")]
        [SerializeField] private float _grayscaleDelay    = 0.3f;

        [Tooltip("背景がグレースケールに変化するのにかかる時間（秒）")]
        [SerializeField] private float _grayscaleDuration = 0.5f;

        [Tooltip("ステージ・敵名・スタミナなどの情報グループがフェードインするのにかかる時間（秒）")]
        [SerializeField] private float _infoFadeDuration  = 0.4f;

        // ─── グレースケール用マテリアル（インスタンス）──────────────
        private Material? _grayscaleMat;

        // ─── 初期化 ──────────────────────────────────────────────
        protected override void OnInitialize()
        {
            if (Data is not GameOverViewData data) return;

            // GAME OVER画像を画面上部に隠す
            if (_gameOverImageRect != null)
            {
                var pos = _gameOverImageRect.anchoredPosition;
                _gameOverImageRect.anchoredPosition = new Vector2(pos.x, pos.y + _dropStartOffsetY);
            }

            // 背景のグレースケールマテリアルを準備（_Grayscale = 0 から始める）
            if (_backgroundImage != null && _backgroundImage.material != null)
            {
                _grayscaleMat = new Material(_backgroundImage.material);
                _grayscaleMat.SetFloat("_Grayscale", 0f);
                _backgroundImage.material = _grayscaleMat;
            }

            // 情報グループは非表示で開始
            if (_infoGroup != null)
            {
                _infoGroup.alpha          = 0f;
                _infoGroup.interactable   = false;
                _infoGroup.blocksRaycasts = false;
            }

            // 各テキスト設定
            _stageNumberText?.GetComponent<GameSys.LocalizedText>()?.SetFormat(data.StageNumber);
            _enemyNameText?.GetComponent<GameSys.LocalizedText>()?.SetFormat(data.EnemyName);

            if (_enemyImage != null)
                _enemyImage.sprite = data.EnemySprite;

            _staminaText?.GetComponent<GameSys.LocalizedText>()?.SetFormat(data.CurrentStamina, data.MaxStamina);

            // スタミナ不足ならリトライボタンを押せないようにする
            if (_btnRetry != null)
                _btnRetry.interactable = data.CurrentStamina >= 1;

            // 回復タイマー購読・初期表示
            if (_recoveryTimerText != null && StaminaManager.isValid)
            {
                StaminaManager.Instance.OnRecoveryTick += UpdateRecoveryTimerText;
                UpdateRecoveryTimerText(StaminaManager.Instance.GetRemainingSeconds());
            }

            _btnRoot?.SetActive(false);
        }

        // ─── 表示アニメーション ──────────────────────────────────
        protected override async UniTask OnOpenAsync()
        {
            // ViewBaseのデフォルト（ViewAnimation）でViewを普通に開く
            await base.OnOpenAsync();

            // 1. GAME OVER画像を落下させる
            if (_gameOverImageRect != null)
            {
                var targetY = _gameOverImageRect.anchoredPosition.y - _dropStartOffsetY;
                await _gameOverImageRect
                    .DOAnchorPosY(targetY, _dropDuration)
                    .SetEase(Ease.OutSine)
                    .SetUpdate(true)
                    .AsyncWaitForCompletion();
            }

            // 落下後の正しい位置をBobの基準点として再設定してからループ開始
            _gameOverBobAnimator?.ResetDefaultPosition();
            _gameOverBobAnimator?.PlayAsync(destroyCancellationToken).Forget();

            // グレースケール前に間を置く
            await UniTask.Delay(
                (int)(_grayscaleDelay * 1000),
                DelayType.Realtime,
                cancellationToken: destroyCancellationToken
            ).SuppressCancellationThrow();
            if (destroyCancellationToken.IsCancellationRequested) return;

            // 2. 背景グレースケール化 ＋ 情報グループフェードインを同時実行
            var seq = DOTween.Sequence().SetUpdate(true);

            if (_grayscaleMat != null)
            {
                seq.Join(DOTween.To(
                    () => _grayscaleMat.GetFloat("_Grayscale"),
                    v  => _grayscaleMat.SetFloat("_Grayscale", v),
                    1f, _grayscaleDuration
                ).SetEase(Ease.InOutSine).SetUpdate(true));
            }

            if (_infoGroup != null)
            {
                seq.Join(
                    DOTween.To(() => _infoGroup.alpha, v => _infoGroup.alpha = v, 1f, _infoFadeDuration)
                        .SetUpdate(true)
                );
            }

            await seq.AsyncWaitForCompletion();

            // 3. 情報グループ操作可能にしてボタン表示
            if (_infoGroup != null)
            {
                _infoGroup.interactable   = true;
                _infoGroup.blocksRaycasts = true;
            }
            _btnRoot?.SetActive(true);
        }

        // ─── 回復タイマー表示 ────────────────────────────────────────
        private void UpdateRecoveryTimerText(int remainingSeconds)
        {
            if (_recoveryTimerText == null) return;
            if (!StaminaManager.isValid || StaminaManager.Instance.Current >= StaminaManager.Instance.Max)
            {
                _recoveryTimerText.text = "";
                return;
            }
            _recoveryTimerText.text = $"{remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
        }

        protected override void OnRelease()
        {
            if (StaminaManager.isValid)
                StaminaManager.Instance.OnRecoveryTick -= UpdateRecoveryTimerText;
        }

        // ─── ボタンハンドラ（Inspector から Button.onClick に登録） ─
        public void OnRetryClicked()
        {
            if (Data is not GameOverViewData data) return;
            var callback = data.OnRetry;
            PopAsync().ContinueWith(callback).Forget();
        }

        public void OnTitleClicked()
        {
            if (Data is not GameOverViewData data) return;
            var callback = data.OnTitle;
            PopAsync().ContinueWith(callback).Forget();
        }

        // 広告視聴後にスタミナ+1する（広告実装後に中身を書く）
        public void OnStaminaRecoverClicked()
        {
        }

        private void OnDestroy()
        {
            if (_grayscaleMat != null)
                Destroy(_grayscaleMat);
        }
    }
}
#nullable disable
