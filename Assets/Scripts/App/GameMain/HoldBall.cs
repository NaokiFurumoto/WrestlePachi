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
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class HoldBall : MonoBehaviour
    {
        // ─── Inspector 参照 ──────────────────────────────────────
        [SerializeField] private SpriteRenderer        _renderer = null!;
        [SerializeField] private TengekiGlowController _glow     = null!;

        // ─── 公開メソッド ────────────────────────────────────────

        /// <summary>
        /// 種別に応じてスプライトとグローを設定する。
        /// sprites のインデックスは HoldType の int 値と対応。
        /// </summary>
        public void Setup(HoldType type, Sprite[] sprites)
        {
            _renderer.sprite = sprites[(int)type];
            ApplyGlow(type);
        }

        /// <summary>スケールポップで出現する。</summary>
        public async UniTask ShowAsync(CancellationToken ct)
        {
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
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

        private void ApplyGlow(HoldType type)
        {
            switch (type)
            {
                case HoldType.Red:    _glow.SetRed();     break;
                case HoldType.Yellow: _glow.SetYellow();  break;
                case HoldType.Green:  _glow.SetGreen();   break;
                case HoldType.Blue:   _glow.SetBlue();    break;
                case HoldType.Black:  _glow.ShowNormal(); break; // Blackはグローなし
            }
        }
    }
}
#nullable disable
