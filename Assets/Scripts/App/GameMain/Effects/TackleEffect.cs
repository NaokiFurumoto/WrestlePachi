#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Effects
{
    /// <summary>
    /// タックル用エフェクト。
    /// 溜め → 突進 → 着地フラッシュ → 衝撃スケールアップ＋フェードアウト。
    /// Sprite・Material・パラメータは Prefab 側で設定する。
    /// </summary>
    public sealed class TackleEffect : MonoBehaviour
    {
        [SerializeField] private Sprite?   _sprite;
        [SerializeField] private Material? _material;
        [SerializeField] private string    _sortingLayerName = "Effect";
        [SerializeField] private int       _sortingOrder     = 0;

        [Header("パラメータ")]
        [SerializeField] private Vector2 _scale          = new Vector2(2.0f, 1.5f);
        [SerializeField] private float   _chargeTime     = 0.08f; // ① 溜め（スケール小→大）
        [SerializeField] private float   _rushTime       = 0.12f; // ② 突進
        [SerializeField] private float   _flashTime      = 0.06f; // ③ 着地フラッシュ
        [SerializeField] private float   _burstTime      = 0.20f; // ④ 衝撃スケールアップ＋フェード
        [SerializeField] private float   _burstScaleMult = 2.0f;  // ④ 最大スケール倍率

        public async UniTask PlayAsync(Vector3 targetPos, CancellationToken ct)
        {
            var sr             = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite           = _sprite;
            sr.material         = _material;
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder     = _sortingOrder;
            sr.color            = new Color(1f, 1f, 1f, 0f);

            var baseScale  = new Vector3(_scale.x, _scale.y, 1f);
            var burstScale = new Vector3(_scale.x * _burstScaleMult, _scale.y * _burstScaleMult, 1f);

            // ① 溜め：フェードインしながらスケール小→大
            transform.localScale = baseScale * 0.3f;
            var chargeSeq = DOTween.Sequence()
                .Join(transform.DOScale(baseScale, _chargeTime).SetEase(Ease.OutQuad))
                .Join(sr.DOFade(1f, _chargeTime));
            using (ct.Register(() => chargeSeq.Kill()))
                await chargeSeq.AsyncWaitForCompletion();

            if (ct.IsCancellationRequested) { Destroy(gameObject); return; }

            // ② 突進：ぷよの中心へ OutExpo で急加速
            var rushTween = transform.DOMove(targetPos, _rushTime).SetEase(Ease.OutExpo);
            using (ct.Register(() => rushTween.Kill()))
                await rushTween.AsyncWaitForCompletion();

            if (ct.IsCancellationRequested) { Destroy(gameObject); return; }

            // ③ 着地フラッシュ：白く光る
            var flashSeq = DOTween.Sequence()
                .Append(sr.DOColor(Color.white, _flashTime * 0.5f).SetEase(Ease.OutQuad))
                .Append(sr.DOColor(new Color(1f, 1f, 1f, 1f), _flashTime * 0.5f));
            using (ct.Register(() => flashSeq.Kill()))
                await flashSeq.AsyncWaitForCompletion();

            if (ct.IsCancellationRequested) { Destroy(gameObject); return; }

            // ④ 衝撃：スケールアップしながらフェードアウト
            var burstSeq = DOTween.Sequence()
                .Join(transform.DOScale(burstScale, _burstTime).SetEase(Ease.OutQuad))
                .Join(sr.DOFade(0f, _burstTime).SetEase(Ease.InQuad));
            using (ct.Register(() => burstSeq.Kill()))
                await burstSeq.AsyncWaitForCompletion();

            Destroy(gameObject);
        }
    }
}
#nullable disable
