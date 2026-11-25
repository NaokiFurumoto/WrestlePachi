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
        private SpriteRenderer?      _chain;

        /// <summary>この玉の保留種別</summary>
        public HoldType Type { get; private set; }

        /// <summary>敵スキル「スキル封印」で封じられた色か（鎖がかかっている）。消化のとき不発で砕ける</summary>
        public bool IsSealed => _chain != null;

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
            Type             = type;
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

        /// <summary>
        /// 敵スキル「スキル封印」の鎖をかける（chain=null なら外す）。delay 秒後にかかる。
        /// 鎖は玉の大きさに合わせて手前に重ね、かかる瞬間に大きいところからガシャンと縮んで締まる。
        /// </summary>
        public void SetSealed(Sprite? chain, float delay = 0f)
        {
            if (chain == null)
            {
                if (_chain != null) Destroy(_chain.gameObject);
                _chain = null;
                return;
            }
            if (_chain != null) return;

            var go = new GameObject("SealChain");
            go.transform.SetParent(transform, false);
            _chain = go.AddComponent<SpriteRenderer>();
            _chain.sprite           = chain;
            _chain.sortingLayerID   = _renderer.sortingLayerID;
            _chain.sortingOrder     = _renderer.sortingOrder + 2;
            // 玉の絵は基準点が足元なので、鎖は絵の中心に重ねる
            if (_renderer.sprite != null) go.transform.localPosition = _renderer.sprite.bounds.center;
            var ballSize  = _renderer.sprite != null ? _renderer.sprite.bounds.size.x : 1f;
            var chainSize = Mathf.Max(chain.bounds.size.x, 0.0001f);
            var scale     = ballSize * 1.15f / chainSize;
            go.transform.localScale = Vector3.zero;
            // 大きいところから一気に締まり、締まった瞬間に玉が揺れて暗く沈む（画面も軽く揺れる）
            DOTween.Sequence()
                .AppendInterval(delay)
                .AppendCallback(() => go.transform.localScale = Vector3.one * scale * 2.2f)
                .Append(go.transform.DOScale(scale, 0.16f).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    ScreenShakeController.Shake(ShakePreset.Light);
                    _renderer.color = new Color(0.55f, 0.55f, 0.6f, 1f);
                    if (_glowRenderer != null) _glowRenderer.enabled = false;
                })
                .Append(transform.DOShakePosition(0.25f, 0.08f, 25))
                .SetLink(go);
        }

        /// <summary>縮小して消滅し、GameObject を Destroy する。封じられた玉は震えてから砕ける（不発）。</summary>
        public async UniTask HideAsync(CancellationToken ct)
        {
            if (IsSealed)
            {
                await CrumbleAsync(ct);
                return;
            }
            transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
            await UniTask.Delay(200, cancellationToken: ct);
            Destroy(gameObject);
        }

        /// <summary>不発：ガタガタ震えて、白く光り、欠片を散らして消える</summary>
        private async UniTask CrumbleAsync(CancellationToken ct)
        {
            transform.DOShakePosition(0.3f, 0.12f, 30).SetLink(gameObject);
            await UniTask.Delay(300, cancellationToken: ct);

            var size = _renderer.sprite != null ? _renderer.sprite.bounds.size.x * transform.lossyScale.x : 0.5f;
            for (var i = 0; i < 6; i++)
            {
                var chip = new GameObject("SealCrumb");
                chip.transform.position   = _renderer.bounds.center;
                chip.transform.localScale = Vector3.one * size * Random.Range(0.18f, 0.28f);
                var sr = chip.AddComponent<SpriteRenderer>();
                sr.sprite         = App.Puyo.FallbackSkillVisuals.SquareSprite;
                sr.color          = i % 2 == 0 ? _renderer.color : new Color(0.6f, 0.62f, 0.68f);
                sr.sortingLayerID = _renderer.sortingLayerID;
                sr.sortingOrder   = _renderer.sortingOrder + 3;
                var angle = (i / 6f + Random.Range(-0.05f, 0.05f)) * Mathf.PI * 2f;
                var to    = _renderer.bounds.center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.6f - 0.4f, 0f) * size * 1.2f;
                DOTween.Sequence()
                    .Append(chip.transform.DOJump(to, size * 0.4f, 1, 0.4f))
                    .Join(chip.transform.DORotate(new Vector3(0f, 0f, Random.Range(-360f, 360f)), 0.4f, RotateMode.FastBeyond360))
                    .Join(sr.DOFade(0f, 0.4f).SetEase(Ease.InQuad))
                    .OnComplete(() => Destroy(chip))
                    .SetLink(chip);
            }
            transform.DOScale(Vector3.zero, 0.12f).SetEase(Ease.InQuad).SetLink(gameObject);
            await UniTask.Delay(120, cancellationToken: ct);
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
