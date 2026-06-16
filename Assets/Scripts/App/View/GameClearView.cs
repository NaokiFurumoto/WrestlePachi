#nullable enable
using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Image用

namespace App
{
    /// <summary>
    /// ステージクリア演出 View。
    /// 演出（ゴング・BGMフェード）は GameClearState 側で完了済み。
    /// Prefab: Resources/Prefabs/Views/GameClearView
    /// </summary>
    public sealed class GameClearView : ViewBase
    {
        // ─── ViewData ────────────────────────────────────────────
        public sealed class GameClearViewData : ViewData
        {
            public int     StageNumber  { get; set; }
            public string  EnemyName    { get; set; } = "";
            public Sprite? EnemySprite  { get; set; }
            public bool    HasNextStage { get; set; }
            public Action? OnNextStage  { get; set; }
            public Action? OnRetry      { get; set; }
        }

        // ─── Inspector フィールド ─────────────────────────────────
        [Header("コンテンツルート（スケールアニメ対象）")]
        [SerializeField] private RectTransform? _contentRect;

        [Header("テキスト")]
        [SerializeField] private TMP_Text? _stageNumberText;
        [SerializeField] private TMP_Text? _enemyNameText;

        [Header("敵演出")]
        [SerializeField] private RectTransform? _enemyRect;
        [SerializeField] private Image?         _enemyImage;
        [SerializeField] private UIAnimator?    _enemyIdleAnimator;

        [Header("撃破マーク")]
        [SerializeField] private RectTransform? _crossMarkRect;

        [Header("ボタン")]
        [SerializeField] private GameObject? _btnRoot;

        [Header("ステージライト")]
        [SerializeField] private StageLightEffect? _stageLightEffect;

        [Header("タイミング（秒）")]
        [SerializeField] private float _revealDuration         = 0.5f;
        [SerializeField] private float _slideInDuration        = 0.7f;
        [SerializeField] private float _crossMarkDelay         = 3f;
        [SerializeField] private float _crossMarkStartScale    = 4f;
        [SerializeField] private float _crossMarkApproachDuration = 0.5f;
        [SerializeField] private float _crossMarkImpactStrength = 0.25f;
        [SerializeField] private float _crossMarkImpactDuration = 0.4f;
        [SerializeField] private float _grayscaleDuration      = 0.6f;

        // ─── グレースケール用マテリアル（インスタンス）──────────────
        private Material? _grayscaleMat;

        // ─── 初期化 ──────────────────────────────────────────────
        protected override void OnInitialize()
        {
            if (Data is not GameClearViewData data) return;

            if (_stageNumberText != null)
                _stageNumberText.text = $"ステージ {data.StageNumber}";

            if (_enemyNameText != null)
                _enemyNameText.text = $"{data.EnemyName}撃破！";

            if (_enemyImage != null && data.EnemySprite != null)
            {
                _enemyImage.sprite = data.EnemySprite;

                if (_enemyImage.material != null)
                {
                    _grayscaleMat = new Material(_enemyImage.material);
                    _grayscaleMat.SetFloat("_Grayscale", 0f);
                    _enemyImage.material = _grayscaleMat;
                }
            }

            // 敵は左外に隠しておく（スライドイン前に見えないようにする）
            _enemyRect?.gameObject.SetActive(false);

            // 撃破マークは非表示で初期化
            if (_crossMarkRect != null)
                _crossMarkRect.localScale = Vector3.zero;

            // ボタン群は撃破マーク後まで非表示
            _btnRoot?.SetActive(false);
        }

        // ─── 表示アニメーション ──────────────────────────────────
        protected override async UniTask OnOpenAsync()
        {
            _stageLightEffect?.PlayAsync(destroyCancellationToken).Forget();

            // View 全体のフェードイン＋スケールアップ
            if (_contentRect != null)
                _contentRect.localScale = Vector3.one * 0.85f;

            var cg  = GetComponent<CanvasGroup>();
            var seq = DOTween.Sequence().SetUpdate(true);
            if (cg != null)
                seq.Join(DOTween.To(() => cg.alpha, v => cg.alpha = v, 1f, _revealDuration).SetUpdate(true));
            if (_contentRect != null)
                seq.Join(_contentRect.DOScale(1f, _revealDuration).SetEase(Ease.OutBack).SetUpdate(true));
            await seq.AsyncWaitForCompletion();

            // 敵をスライドイン
            await SlideInEnemyAsync();

            // 待機アニメ開始
            _enemyIdleAnimator?.PlayAsync(destroyCancellationToken).Forget();

            // 一定時間後に撃破マーク演出
            await UniTask.Delay(
                (int)(_crossMarkDelay * 1000),
                DelayType.Realtime,
                cancellationToken: destroyCancellationToken
            ).SuppressCancellationThrow();

            if (destroyCancellationToken.IsCancellationRequested) return;

            // 敵の動きをその場で止める（位置リセットしない）
            _enemyIdleAnimator?.Pause();

            await ShowCrossMarkAsync();
        }

        // ─── 敵スライドイン ─────────────────────────────────────
        private async UniTask SlideInEnemyAsync()
        {
            if (_enemyRect == null) return;

            var canvas    = GetComponentInParent<Canvas>();
            float canvasW = canvas != null
                ? canvas.GetComponent<RectTransform>().rect.width
                : 1080f;
            float endX    = _enemyRect.anchoredPosition.x;
            float startX  = -(canvasW * 0.5f + _enemyRect.rect.width * 0.5f + 50f);

            _enemyRect.anchoredPosition = new Vector2(startX, _enemyRect.anchoredPosition.y);
            _enemyRect.gameObject.SetActive(true);

            await _enemyRect
                .DOAnchorPosX(endX, _slideInDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
        }

        // ─── 撃破マーク演出 ──────────────────────────────────────
        private async UniTask ShowCrossMarkAsync()
        {
            if (_crossMarkRect == null) return;

            // 大きい状態からスタートして縮小しながら近づいてくる
            _crossMarkRect.localScale = Vector3.one * _crossMarkStartScale;

            await _crossMarkRect
                .DOScale(1f, _crossMarkApproachDuration)
                .SetEase(Ease.OutExpo)
                .SetUpdate(true)
                .AsyncWaitForCompletion();

            // 着地の衝撃
            await _crossMarkRect
                .DOPunchScale(Vector3.one * _crossMarkImpactStrength, _crossMarkImpactDuration, 8, 1f)
                .SetUpdate(true)
                .AsyncWaitForCompletion();

            // 着地と同時にグレースケール化
            if (_grayscaleMat != null)
            {
                DOTween.To(
                    () => _grayscaleMat.GetFloat("_Grayscale"),
                    v  => _grayscaleMat.SetFloat("_Grayscale", v),
                    1f,
                    _grayscaleDuration
                ).SetEase(Ease.InOutSine).SetUpdate(true);
            }

            _btnRoot?.SetActive(true);
        }

        // ─── ボタンハンドラ（Inspectorから Button.onClick に登録） ───
        public void OnNextStageClicked()
        {
            if (Data is not GameClearViewData data) return;
            var callback = data.OnNextStage;
            PopAsync().ContinueWith(callback).Forget();
        }

        public void OnRetryClicked()
        {
            if (Data is not GameClearViewData data) return;
            var callback = data.OnRetry;
            PopAsync().ContinueWith(callback).Forget();
        }

        private void OnDestroy()
        {
            if (_grayscaleMat != null)
                Destroy(_grayscaleMat);
        }
    }
}
#nullable disable
