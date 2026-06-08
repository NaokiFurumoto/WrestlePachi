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
    /// ちびキャラが左からスライドインし、攻撃アニメーション再生後に右へスライドアウトする。
    /// Prefab: Resources/Prefabs/Views/SkillCutInView
    /// </summary>
    public sealed class SkillCutInView : ViewBase
    {
        /// <summary>スキルごとに差し替えるデータ。PushViewAsync に渡す。</summary>
        public class SkillCutInViewData : ViewData
        {
            public RuntimeAnimatorController? Controller  { get; set; }
            public Vector2                    ImageSize   { get; set; }
        }

        [Header("スライド対象")]
        [SerializeField] private RectTransform? _chibiContainer;

        [Header("ちびキャラ")]
        [SerializeField] private ChibiCharacter? _chibiCharacter;
        [SerializeField] private RectTransform?  _chibiImageRect;

        [Header("帯エフェクト")]
        [SerializeField] private GameObject? _lineRoot;

        [Header("スライド設定")]
        [SerializeField] private float _slideInDuration  = 0.15f;
        [SerializeField] private float _slideOutDuration = 0.15f;
        [SerializeField] private float _offScreenX       = 1400f;

        // ─── ViewBase override ────────────────────────────────────────

        /// <summary>ViewData から Controller・サイズ・帯エフェクト表示をセットする。</summary>
        protected override void OnInitialize()
        {
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

            // ViewBase.OpenAsync が alpha=0 にリセットするので戻す
            var cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;

            _chibiContainer.anchoredPosition = new Vector2(-_offScreenX, 0f);
            await _chibiContainer
                .DOAnchorPosX(0f, _slideInDuration)
                .SetEase(Ease.OutCubic)
                .AsyncWaitForCompletion();
        }

        /// <summary>センターから右画面外へスライドアウト。</summary>
        protected override async UniTask OnCloseAsync()
        {
            if (_chibiContainer == null) return;

            await _chibiContainer
                .DOAnchorPosX(_offScreenX, _slideOutDuration)
                .SetEase(Ease.InCubic)
                .AsyncWaitForCompletion();
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
