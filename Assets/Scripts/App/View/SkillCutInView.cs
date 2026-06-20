#nullable enable
using System.Threading;
using App.Skills;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameSys;
using UnityEngine;

namespace App
{
    /// <summary>
    /// スキル発動時のカットイン演出を管理する View。
    /// ちびキャラが左からスライドインし、攻撃アニメーション再生後に閉じる。
    /// 閉じ方は CloseStyle で切り替え可能。
    /// Prefab: Resources/Prefabs/Views/SkillCutInView
    /// </summary>
    public sealed class SkillCutInView : ViewBase
    {
        public enum CutInCloseStyle { SlideRight, ZoomFade }

        /// <summary>スキルごとに差し替えるデータ。PushViewAsync に渡す。</summary>
        public class SkillCutInViewData : ViewData
        {
            public RuntimeAnimatorController? Controller  { get; set; }
            public Vector2                    ImageSize   { get; set; }
            public CutInCloseStyle            CloseStyle  { get; set; } = CutInCloseStyle.SlideRight;
        }

        [Header("スライド対象")]
        [SerializeField] private RectTransform? _chibiContainer;

        [Header("ちびキャラ")]
        [SerializeField] private ChibiCharacter? _chibiCharacter;
        [SerializeField] private RectTransform?  _chibiImageRect;

        [Header("帯エフェクト")]
        [SerializeField] private GameObject? _lineRoot;

        private CanvasGroup? _canvasGroup;

        [Header("スライド設定")]
        [SerializeField] private float _slideInDuration  = 0.15f;
        [SerializeField] private float _slideOutDuration = 0.15f;
        [SerializeField] private float _offScreenX       = 1400f;

        [Header("ズームフェード設定")]
        [SerializeField] private float _zoomFadeScale    = 1.5f;
        [SerializeField] private float _zoomFadeDuration = 0.4f;

        // ─── ViewBase override ────────────────────────────────────────

        /// <summary>ViewData から Controller・サイズ・帯エフェクト表示をセットする。</summary>
        protected override void OnInitialize()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            if (Data is not SkillCutInViewData data) return;
            if (data.Controller != null)
                _chibiCharacter?.SetController(data.Controller);

            if (_chibiImageRect != null)
                _chibiImageRect.sizeDelta = data.ImageSize;

            if (_lineRoot != null) _lineRoot.SetActive(true);
        }

        /// <summary>左画面外からセンターへスライドイン。</summary>
        protected override async UniTask OnOpenAsync()
        {
            if (_chibiContainer == null) return;

            AppSound.PlayCutIn();
            // ViewBase.OpenAsync が alpha=0 にリセットするので戻す
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            _chibiContainer.anchoredPosition = new Vector2(-_offScreenX, 0f);
            await _chibiContainer
                .DOAnchorPosX(0f, _slideInDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)   // timeScale=0 でもスライドが止まらないよう非依存に
                .AsyncWaitForCompletion();
        }

        protected override async UniTask OnCloseAsync()
        {
            var style = (Data as SkillCutInViewData)?.CloseStyle ?? CutInCloseStyle.SlideRight;

            if (style == CutInCloseStyle.ZoomFade)
                await PlayZoomFadeAsync();
            else
                await PlaySlideRightAsync();
        }

        // ─── 閉じアニメーション ───────────────────────────────────────

        private async UniTask PlaySlideRightAsync()
        {
            if (_chibiContainer == null) return;

            await _chibiContainer
                .DOAnchorPosX(_offScreenX, _slideOutDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
        }

        private async UniTask PlayZoomFadeAsync()
        {
            if (_chibiContainer == null) return;

            var seq = DOTween.Sequence().SetUpdate(true);
            seq.Join(_chibiContainer.DOScale(_zoomFadeScale, _zoomFadeDuration).SetEase(Ease.OutQuad));
            if (_canvasGroup != null)
                seq.Join(DOTween.To(() => _canvasGroup.alpha, v => _canvasGroup.alpha = v, 0f, _zoomFadeDuration).SetEase(Ease.InQuad));
            await seq.AsyncWaitForCompletion();
        }

        // ─── 公開API ──────────────────────────────────────────────────

        /// <summary>攻撃アニメーションを再生する。Open/Close は呼び出し側が担う。</summary>
        public async UniTask PlayAttackAsync(CancellationToken ct)
        {
            await (_chibiCharacter?.PlayAttackAsync(ct) ?? UniTask.CompletedTask);
        }
    }
}
#nullable disable
