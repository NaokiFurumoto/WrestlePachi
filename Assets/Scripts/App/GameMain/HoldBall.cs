#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 保留玉1個を表示・アニメーションするコンポーネント。
    /// HoldDisplay から Instantiate され、Setup() で種別を設定される。
    /// オーラは MaterialPropertyBlock で _AuraColor を直接制御する。
    /// TengekiGlowController には依存しない。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class HoldBall : MonoBehaviour
    {
        // ─── Inspector 参照 ──────────────────────────────────────
        [SerializeField] private SpriteRenderer _renderer     = null!;
        [SerializeField] private SpriteRenderer _glowRenderer = null!; // Grow 子オブジェクト

        // ─── オーラ色テーブル（HoldType の int 値と対応）───────────
        private static readonly Color[] s_auraColors =
        {
            new Color(1.0f, 0.15f, 0.1f),  // Red
            new Color(1.0f, 0.85f, 0.0f),  // Yellow
            new Color(0.1f, 1.0f, 0.25f),  // Green
            new Color(0.2f, 0.5f,  1.0f),  // Blue
            new Color(0.7f, 0.1f,  1.0f),  // Purple
            Color.clear,                    // Black: alpha=0 → オーラなし
            new Color(1.0f, 0.5f,  0.9f),  // Rainbow
        };

        private static readonly int s_auraColorId = Shader.PropertyToID("_AuraColor");

        // ─── 内部状態 ────────────────────────────────────────────
        private Vector3              _defaultScale;
        private MaterialPropertyBlock _mpb = null!;

        private void Awake()
        {
            _defaultScale = transform.localScale;
            _mpb          = new MaterialPropertyBlock();
        }

        // ─── 公開メソッド ────────────────────────────────────────

        /// <summary>
        /// 種別に応じてスプライトとオーラ色を設定する。
        /// sprites のインデックスは HoldType の int 値と対応。
        /// </summary>
        public void Setup(HoldType type, Sprite[] sprites)
        {
            var index        = (int)type;
            var sprite       = index < sprites.Length ? sprites[index] : sprites[0];
            _renderer.sprite = sprite;

            if (_glowRenderer != null)
            {
                _glowRenderer.sprite = sprite;
                ApplyAura(type);
            }
        }

        /// <summary>スケールポップで出現する。アニメーション先は Prefab のデフォルトスケール。</summary>
        public async UniTask ShowAsync(CancellationToken ct)
        {
            transform.localScale = Vector3.zero;
            transform.DOScale(_defaultScale, 0.3f).SetEase(Ease.OutBack);
            await UniTask.Delay(300, cancellationToken: ct);
        }

        /// <summary>指定スロットの子に移動し、位置を (0,0,0) にアニメーションする。</summary>
        public async UniTask MoveToSlotAsync(Transform targetSlot, CancellationToken ct)
        {
            transform.SetParent(targetSlot);
            transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.InOutQuad);
            await UniTask.Delay(200, cancellationToken: ct);
        }

        /// <summary>縮小して消滅し、GameObject を Destroy する。</summary>
        public async UniTask HideAsync(CancellationToken ct)
        {
            transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
            await UniTask.Delay(200, cancellationToken: ct);
            Destroy(gameObject);
        }

        // ─── 内部処理 ────────────────────────────────────────────

        /// <summary>HoldType に対応するオーラ色を MaterialPropertyBlock で設定する。</summary>
        private void ApplyAura(HoldType type)
        {
            var index   = (int)type;
            var hasAura = index < s_auraColors.Length && s_auraColors[index].a > 0f;

            _glowRenderer.enabled = hasAura;
            if (!hasAura) return;

            _glowRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(s_auraColorId, s_auraColors[index]);
            _glowRenderer.SetPropertyBlock(_mpb);
        }
    }
}
#nullable disable
