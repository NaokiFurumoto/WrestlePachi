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
        [SerializeField] private Material?       _dissolveMaterial;
        [SerializeField] private SpriteRenderer? _glowRenderer;

        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

        private SpriteRenderer _renderer;
        private Material?      _runtimeMaterial;

        /// <summary>このぷよの色</summary>
        public PuyoColor Color { get; private set; }

        /// <summary>グリッド上のセル座標 (列, 行)</summary>
        public Vector2Int Cell { get; set; }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            // 起動時はグロー非表示
            if (_glowRenderer != null)
                _glowRenderer.color = new UnityEngine.Color(1f, 1f, 1f, 0f);
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        }

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
        /// スプライトのみを差し替える（色・当たり判定は変えない）。
        /// 岩のヒット演出など、見た目だけ変えたい場合に使う。
        /// </summary>
        public void SetSprite(Sprite sprite) => _renderer.sprite = sprite;

        /// <summary>
        /// 本体の色を変える（敵スキルの仮表示用：爆弾は赤、岩は灰色など）。
        /// 点滅などの演出は開始時の色を覚えて最後に戻すので、ここで設定した色は演出後も残る。
        /// </summary>
        public void SetTint(UnityEngine.Color color) => _renderer.color = color;

        /// <summary>消えるとき、溶けるかわりにガラスが割れて砕ける（お邪魔ぷよ用）</summary>
        private bool  _shatterOnClear;
        private float _cellSize = 1f;

        /// <summary>
        /// お邪魔ぷよをガラス玉にする：ガラスのマテリアルで描き（未設定なら通常描画のまま）、
        /// 消えるときはガラスが割れて破片が飛び散るようにする。
        /// </summary>
        public void MakeGlass(Material? glassMaterial, float cellSize)
        {
            if (glassMaterial != null) _renderer.sharedMaterial = glassMaterial;
            _shatterOnClear = true;
            _cellSize       = cellSize;
        }

        /// <summary>
        /// お邪魔ぷよ着地時のバウンス演出。
        /// </summary>
        public void PlayLandBounce()
            => transform.DOPunchPosition(Vector3.up * 0.15f, 0.3f, 3, 0f);

        /// <summary>
        /// 揃った時の背面グロー演出（約300ms）。
        /// PuyoBoard から ClearAsync の前に並列呼び出しする。
        /// </summary>
        public async UniTask FlashMatchedAsync(CancellationToken ct)
        {
            if (_glowRenderer == null) return;

            // ぷよと同じスプライトを背面レンダラーにセット
            _glowRenderer.sprite               = _renderer.sprite;
            _glowRenderer.transform.localScale = Vector3.one;

            var glowColor = PuyoColorToGlowColor(Color);
            glowColor.a = 1f;
            _glowRenderer.color = glowColor;

            // ポヨン：スケールパンチ（グロー全体に適用）
            transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 3, 1f);

            // ぷよ本体: 発光色に染まる（0.15秒）→ 白に戻る（0.35秒）
            var baseColor = _renderer.color;
            _renderer.DOColor(glowColor, 0.15f).SetEase(Ease.OutQuad);

            // 背面グロー: scale 1.0→1.5 ＋ alpha 1→0（0.45秒）
            _glowRenderer.transform.DOScale(1.5f, 0.45f).SetEase(Ease.OutQuad);
            _glowRenderer.DOFade(0f, 0.45f).SetEase(Ease.InQuad);

            await UniTask.Delay(200, cancellationToken: ct).SuppressCancellationThrow();

            // ぷよ本体を元色に戻す
            _renderer.DOColor(baseColor, 0.35f).SetEase(Ease.InQuad);

            await UniTask.Delay(350, cancellationToken: ct).SuppressCancellationThrow();

            // キャンセル時も確実にリセット
            _renderer.color                    = baseColor;
            _glowRenderer.color                = new UnityEngine.Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            _glowRenderer.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 消去アニメーションを再生して自身を破棄する（IClearable実装）。
        /// Dissolve シェーダーがアサインされていれば溶けて消える演出、なければ従来のスケール演出。
        /// </summary>
        public async UniTask ClearAsync(CancellationToken ct)
        {
            if (_shatterOnClear)
            {
                await ClearWithShatterAsync(ct);
            }
            else if (_dissolveMaterial != null)
            {
                await ClearWithDissolveAsync(ct);
            }
            else
            {
                await ClearWithScaleAsync(ct);
            }

            // 消える演出の間に別の処理（デバッグの盤面リセットなど）で先に消されていたら何もしない
            if (this == null) return;
            Destroy(gameObject);
        }

        // ── ガラスが割れて砕ける（お邪魔ぷよ） ──────────────────────

        private async UniTask ClearWithShatterAsync(CancellationToken ct)
        {
            // 一瞬ピキッと膨らんでから、パリンと割れて破片が飛び散る
            transform.DOScale(transform.localScale * 1.12f, 0.06f).SetEase(Ease.OutQuad);
            await UniTask.Delay(60, cancellationToken: ct).SuppressCancellationThrow();
            GlassShatterEffect.Spawn(transform.position, _cellSize, _renderer, transform.parent);
            _renderer.enabled = false;
            await UniTask.Delay(60, cancellationToken: ct).SuppressCancellationThrow();
        }

        // ── Dissolve シェーダーで消える ──────────────────────────────

        private async UniTask ClearWithDissolveAsync(CancellationToken ct)
        {
            // マテリアルをインスタンス化してスプライトのテクスチャをセット
            _runtimeMaterial = Instantiate(_dissolveMaterial);
            _runtimeMaterial.SetTexture("_MainTex", _renderer.sprite.texture);
            _renderer.material = _runtimeMaterial;

            // _Dissolve を 0 → 1 にアニメーション（0.35秒）
            var dissolveValue = 0f;
            DOTween.To(
                () => dissolveValue,
                v  => { dissolveValue = v; _runtimeMaterial.SetFloat(DissolveId, v); },
                1f, 0.35f
            ).SetEase(Ease.InQuad);

            await UniTask.Delay(350, cancellationToken: ct).SuppressCancellationThrow();
        }

        // ── PuyoColor → 発光色 ──────────────────────────────────────

        private static UnityEngine.Color PuyoColorToGlowColor(PuyoColor color) => color switch
        {
            PuyoColor.RED    => new UnityEngine.Color(1.0f, 0.20f, 0.08f),
            PuyoColor.BLUE   => new UnityEngine.Color(0.15f, 0.50f, 1.0f),
            PuyoColor.GREEN  => new UnityEngine.Color(0.10f, 1.0f, 0.28f),
            PuyoColor.YELLOW => new UnityEngine.Color(1.0f, 0.88f, 0.10f),
            PuyoColor.PURPLE => new UnityEngine.Color(0.65f, 0.10f, 1.0f),
            _                => UnityEngine.Color.white,
        };

        // ── 従来のスケール演出（フォールバック） ──────────────────────

        private async UniTask ClearWithScaleAsync(CancellationToken ct)
        {
            transform.DOScale(1.3f, 0.10f).SetEase(Ease.OutQuad);
            await UniTask.Delay(100, cancellationToken: ct);

            transform.DOScale(0f, 0.15f).SetEase(Ease.InQuad);
            await UniTask.Delay(150, cancellationToken: ct);
        }
    }
}
