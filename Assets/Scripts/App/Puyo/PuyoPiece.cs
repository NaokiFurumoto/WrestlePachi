using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>
    /// 消去可能なオブジェクトの共通インターフェース。
    /// 通常ぷよ・お邪魔ぷよで消去アニメーションを切り替えられるよう抽象化する。
    /// </summary>
    public interface IClearable
    {
        /// <summary>消去アニメーションを再生し、完了後に自身を破棄する</summary>
        UniTask ClearAsync(CancellationToken ct);
    }

    /// <summary>
    /// ぷよ1個を表すコンポーネント。
    /// 色・スプライト表示・消去アニメーションを担当する。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PuyoPiece : MonoBehaviour, IClearable
    {
        private SpriteRenderer _renderer;

        /// <summary>このぷよの色</summary>
        public PuyoColor Color { get; private set; }

        /// <summary>グリッド上のセル座標 (列, 行)</summary>
        public Vector2Int Cell { get; set; }

        private void Awake() => _renderer = GetComponent<SpriteRenderer>();

        /// <summary>
        /// 色とスプライトを設定して初期化する。
        /// Instantiate直後に必ず呼ぶこと。
        /// </summary>
        public void Setup(PuyoColor color, Sprite sprite)
        {
            Color            = color;
            _renderer.sprite = sprite;
        }

        /// <summary>
        /// 消去アニメーションを再生して自身を破棄する（IClearable実装）。
        /// ポップアップ → 縮小して消える演出。
        /// DOTweenを発火しつつ UniTask.Delay で完了を待つ。
        /// </summary>
        public async UniTask ClearAsync(CancellationToken ct)
        {
            // 少し膨らんでから縮んで消える（ぷよらしい弾力演出）
            transform.DOScale(1.3f, 0.10f).SetEase(Ease.OutQuad);
            await UniTask.Delay(100, cancellationToken: ct);

            transform.DOScale(0f, 0.15f).SetEase(Ease.InQuad);
            await UniTask.Delay(150, cancellationToken: ct);

            Destroy(gameObject);
        }
    }
}
