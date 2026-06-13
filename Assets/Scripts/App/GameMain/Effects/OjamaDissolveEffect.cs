#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Effects
{
    /// <summary>
    /// お邪魔ぷよ専用 Dissolve 消滅エフェクト。
    /// Instantiate 後に PlayAsync(sprite, ct) を呼ぶと粉々に崩れて自己 Destroy する。
    /// Material（S_OjamaDissolve）は Prefab 側で設定する。
    /// </summary>
    public sealed class OjamaDissolveEffect : MonoBehaviour
    {
        [SerializeField] private Material? _material;
        [SerializeField] private string    _sortingLayerName = "Effect";
        [SerializeField] private int       _sortingOrder     = 0;
        [SerializeField] private float     _duration         = 1.5f;

        public async UniTask PlayAsync(Sprite sprite, CancellationToken ct)
        {
            var sr             = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite           = sprite;
            sr.material         = _material; // Unity が自動でインスタンス化
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder     = _sortingOrder;

            var mat = sr.material;
            mat.SetFloat("_Cutoff", 0f);

            var tween = DOTween.To(
                () => mat.GetFloat("_Cutoff"),
                v  => mat.SetFloat("_Cutoff", v),
                1f, _duration).SetEase(Ease.InQuad);

            using (ct.Register(() => tween.Kill()))
                await tween.AsyncWaitForCompletion();

            Destroy(gameObject);
        }
    }
}
#nullable disable
