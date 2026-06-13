#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Effects
{
    /// <summary>
    /// 縦方向スイープエフェクト（ヒップドロップ用）。
    /// ArrowSweepEffect の縦版。3ウェーブが時差で上→下に走る。
    /// Sprite・Material・ウェーブパラメータは Prefab 側で設定する。
    /// </summary>
    public sealed class VerticalSweepEffect : MonoBehaviour
    {
        [Serializable]
        private struct WaveParam
        {
            [Tooltip("スケール（先頭ほど小さく）")]
            public float Scale;
            [Tooltip("下方向の移動距離（先頭ほど長い）")]
            public float Distance;
            [Tooltip("アニメーション時間")]
            public float Duration;
            [Tooltip("このウェーブの発生ディレイ")]
            public float Delay;
        }

        [SerializeField] private Sprite?   _sprite;
        [SerializeField] private Material? _material;
        [SerializeField] private string    _sortingLayerName = "Effect";
        [SerializeField] private int       _sortingOrder     = 100;

        [SerializeField] private WaveParam[] _waves =
        {
            new WaveParam { Scale = 0.7f, Distance = 3.0f, Duration = 0.18f, Delay = 0.00f },
            new WaveParam { Scale = 1.0f, Distance = 2.5f, Duration = 0.23f, Delay = 0.04f },
            new WaveParam { Scale = 1.3f, Distance = 2.0f, Duration = 0.30f, Delay = 0.08f },
        };

        public async UniTaskVoid PlayAsync(CancellationToken ct)
        {
            var tasks = new UniTask[_waves.Length];
            for (var i = 0; i < _waves.Length; i++)
                tasks[i] = PlayWaveAsync(_waves[i], ct);

            await UniTask.WhenAll(tasks);
            Destroy(gameObject);
        }

        private async UniTask PlayWaveAsync(WaveParam wave, CancellationToken ct)
        {
            if (wave.Delay > 0f)
                await UniTask.Delay(
                    Mathf.RoundToInt(wave.Delay * 1000),
                    cancellationToken: ct).SuppressCancellationThrow();

            var go = new GameObject("FX_Wave");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = new Vector3(wave.Scale, wave.Scale, 1f);

            var sr             = go.AddComponent<SpriteRenderer>();
            sr.sprite           = _sprite;
            sr.material         = _material;
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder     = _sortingOrder;

            var targetPos = transform.position + Vector3.down * wave.Distance;
            var seq = DOTween.Sequence()
                .Join(go.transform.DOMove(targetPos, wave.Duration).SetEase(Ease.OutQuad))
                .Join(sr.DOColor(new Color(1f, 1f, 1f, 0f), wave.Duration));

            using var reg = ct.Register(() => seq.Kill());
            await seq.AsyncWaitForCompletion();

            if (go != null) Destroy(go);
        }
    }
}
#nullable disable
