#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace App.Effects
{
    /// <summary>
    /// コンボ表示時の花火エフェクト。
    /// ComboView から Instantiate → PlayAsync(chainCount, ct) で自己 Destroy する。
    ///
    /// Prefab 構成: root に ComboFireworkEffect のみ。
    /// Inspector: _particleSprites に円・星など複数スプライトを設定。各スパークがランダムに選ぶ。
    /// </summary>
    public sealed class ComboFireworkEffect : MonoBehaviour
    {
        [SerializeField] private Sprite[]  _particleSprites = System.Array.Empty<Sprite>();
        [SerializeField] private Material? _particleMaterial;
        [SerializeField] private string    _sortingLayerName = "Effect";
        [SerializeField] private int       _sortingOrder     = 5;

        [Header("パーティクル設定")]
        [SerializeField] private float _speed         = 2.5f;
        [SerializeField] private float _duration      = 0.6f;
        [SerializeField] private float _particleScale = 0.35f;

        // 連鎖数に応じて使う色数が増える
        private static readonly Color[] FireworkColors =
        {
            new Color(1.0f, 0.9f, 0.1f), // Gold
            new Color(1.0f, 0.4f, 0.0f), // Orange
            new Color(1.0f, 0.2f, 0.5f), // Pink
            new Color(0.2f, 0.8f, 1.0f), // Cyan
            new Color(0.8f, 0.2f, 1.0f), // Purple
            new Color(0.2f, 1.0f, 0.4f), // Green
        };

        public async UniTaskVoid PlayAsync(int chainCount, CancellationToken ct)
        {
            // 連鎖数が多いほどスパーク数・色数が増える（上限16）
            var sparkCount = Mathf.Min(6 + chainCount * 2, 16);
            var colorCount = Mathf.Clamp(chainCount - 1, 1, FireworkColors.Length);
            var tasks      = new UniTask[sparkCount];

            for (var i = 0; i < sparkCount; i++)
            {
                var angle = 360f / sparkCount * i + Random.Range(-15f, 15f);
                var color = FireworkColors[i % colorCount];
                tasks[i]  = PlaySparkAsync(angle, color, ct);
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask PlaySparkAsync(float angleDeg, Color color, CancellationToken ct)
        {
            var go = new GameObject("FX_Spark");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.zero;

            var sr             = go.AddComponent<SpriteRenderer>();
            sr.sprite           = _particleSprites.Length > 0
                                    ? _particleSprites[Random.Range(0, _particleSprites.Length)]
                                    : null;
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder     = _sortingOrder;
            if (_particleMaterial != null)
            {
                sr.material = Instantiate(_particleMaterial);
                sr.material.SetColor("_GlowColor", color);
            }
            else
            {
                sr.color = color;
            }

            var rad    = angleDeg * Mathf.Deg2Rad;
            var dir    = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            var spd    = _speed         * Random.Range(0.8f, 1.2f);
            var dur    = _duration      * Random.Range(0.85f, 1.15f);
            var size   = _particleScale * Random.Range(0.7f, 1.3f);
            var endPos = transform.position + dir * spd;

            var seq = DOTween.Sequence();
            // スケール：膨らんで消える
            seq.Append(go.transform.DOScale(size, dur * 0.25f).SetEase(Ease.OutQuad));
            seq.Insert(dur * 0.25f, go.transform.DOScale(0f, dur * 0.75f).SetEase(Ease.InQuad));
            // 放射状に飛ぶ
            seq.Insert(0f, go.transform.DOMove(endPos, dur).SetEase(Ease.OutCubic));
            // 後半からフェードアウト
            seq.Insert(dur * 0.4f, sr.DOFade(0f, dur * 0.6f));

            using var reg = ct.Register(() => seq.Kill());
            await seq.AsyncWaitForCompletion();

            if (go != null) Destroy(go);
        }
    }
}
#nullable disable
