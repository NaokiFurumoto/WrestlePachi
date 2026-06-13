#nullable enable
using System.Threading;
using App.Puyo;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace App.Effects
{
    /// <summary>
    /// ぷよ爆散パーティクルエフェクト。
    /// Instantiate 後に PlayAsync(color, ct) を呼ぶと放射状に粒子を飛ばして自己 Destroy する。
    /// スプライト・マテリアル・各パラメータは Prefab 側で設定する。
    /// </summary>
    public sealed class PuyoBurstEffect : MonoBehaviour
    {
        [SerializeField] private Sprite?   _particleSprite;
        [SerializeField] private Material? _particleMaterial;
        [SerializeField] private string    _sortingLayerName = "Effect";
        [SerializeField] private int       _sortingOrder     = 0;

        [Header("パーティクル設定")]
        [SerializeField] private int   _particleCount = 8;
        [SerializeField] private float _speed         = 1.5f;
        [SerializeField] private float _duration      = 1.2f;
        [SerializeField] private float _gravity       = -2f;
        [SerializeField] private float _particleScale = 1.2f;

        // PuyoColor のインデックス順に対応（RED=0 ... OJAMA=5）
        private static readonly Color[] GlowColors =
        {
            new Color(1.0f, 0.2f, 0.0f), // RED
            new Color(0.0f, 0.5f, 1.0f), // BLUE
            new Color(0.0f, 1.0f, 0.3f), // GREEN
            new Color(1.0f, 0.8f, 0.0f), // YELLOW
            new Color(0.7f, 0.0f, 1.0f), // PURPLE
            new Color(0.8f, 0.8f, 0.8f), // OJAMA
        };

        public async UniTaskVoid PlayAsync(PuyoColor color, CancellationToken ct)
        {
            var tasks = new UniTask[_particleCount];
            for (var i = 0; i < _particleCount; i++)
            {
                // 均等角度に ±20° のランダムぶれを加える
                var angle = 360f / _particleCount * i + Random.Range(-20f, 20f);
                tasks[i] = PlayParticleAsync(angle, color, ct);
            }
            await UniTask.WhenAll(tasks);
            Destroy(gameObject);
        }

        private async UniTask PlayParticleAsync(float angleDeg, PuyoColor color, CancellationToken ct)
        {
            var go       = new GameObject("FX_Particle");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one * (_particleScale * Random.Range(0.7f, 1.3f));
            var startPos = transform.position;

            var sr             = go.AddComponent<SpriteRenderer>();
            sr.sprite           = _particleSprite;
            sr.material         = _particleMaterial; // Unity が自動でインスタンス化する
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder     = _sortingOrder;

            // ぷよの色に合わせて発光色を設定
            var glowColor = (int)color < GlowColors.Length ? GlowColors[(int)color] : Color.white;
            sr.material.SetColor("_GlowColor", glowColor);

            var rad = angleDeg * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            var spd = _speed    * Random.Range(0.8f, 1.2f);
            var dur = _duration * Random.Range(0.85f, 1.15f);

            // 放物線の中間点・終点を物理計算で算出
            var midT   = dur * 0.4f;
            var midPos = new Vector3(
                startPos.x + dir.x * spd * midT,
                startPos.y + dir.y * spd * midT + 0.5f * _gravity * midT * midT,
                0f);
            var endPos = new Vector3(
                startPos.x + dir.x * spd * dur,
                startPos.y + dir.y * spd * dur + 0.5f * _gravity * dur * dur,
                0f);

            var seq = DOTween.Sequence()
                .Join(go.transform.DOPath(
                        new[] { midPos, endPos }, dur, PathType.CatmullRom)
                    .SetEase(Ease.Linear))
                .Join(sr.DOColor(new Color(1f, 1f, 1f, 0f), dur)
                    .SetEase(Ease.InQuad));

            using var reg = ct.Register(() => seq.Kill());
            await seq.AsyncWaitForCompletion();

            if (go != null) Destroy(go);
        }
    }
}
#nullable disable
