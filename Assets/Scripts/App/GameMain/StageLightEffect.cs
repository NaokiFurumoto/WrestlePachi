#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
namespace App
{
    /// <summary>
    /// ゲームクリア演出で使うステージスポットライトエフェクト。
    /// GameClearView の Prefab 内に置き、OnOpenAsync() から PlayAsync() を呼ぶ。
    /// </summary>
    public sealed class StageLightEffect : MonoBehaviour
    {
        [Serializable]
        private struct LightData
        {
            [Tooltip("ライトの GameObject（Image コンポーネント付き）")]
            public GameObject LightObject;
            [Tooltip("揺れの速さ（秒/往復）")]   public float SwayDuration;
            [Tooltip("揺れの初期位相（0〜1）")] public float SwayPhaseOffset;
        }

        [SerializeField] private LightData[] _lights        = Array.Empty<LightData>();
        [SerializeField] private float        _swayAngle     = 30f;
        [SerializeField] private float        _fadeInDuration  = 0.6f;
        [SerializeField] private float        _fadeOutDuration = 0.5f;

        private void Awake()
        {
            foreach (var l in _lights)
            {
                var img = l.LightObject?.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        /// <summary>
        /// フェードイン → 揺れループを開始する。
        /// CT がキャンセルされるとフェードアウトして完了する。
        /// </summary>
        public async UniTask PlayAsync(CancellationToken ct)
        {
            // 時間差でフェードイン
            for (var i = 0; i < _lights.Length; i++)
            {
                var img = _lights[i].LightObject?.GetComponent<UnityEngine.UI.Image>();
                if (img == null) continue;
                img.DOFade(1f, _fadeInDuration)
                   .SetDelay(i * 0.08f)
                   .SetEase(Ease.OutCubic);
            }

            await UniTask.Delay(
                Mathf.RoundToInt((_fadeInDuration + _lights.Length * 0.08f) * 1000),
                DelayType.Realtime, cancellationToken: ct).SuppressCancellationThrow();

            if (ct.IsCancellationRequested) return;

            // 各ライトで独立した揺れループ
            var swaySequences = new Sequence[_lights.Length];
            for (var i = 0; i < _lights.Length; i++)
            {
                var l = _lights[i];
                if (l.LightObject == null) continue;

                var t        = l.LightObject.transform;
                var duration = Mathf.Abs(l.SwayDuration);
                if (duration < 0.1f) duration = 2f; // 0 や極小値を防ぐ
                var angle    = _swayAngle;

                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-angle, angle, l.SwayPhaseOffset));

                var seq = DOTween.Sequence();
                seq.Append(t.DOLocalRotate(new Vector3(0f, 0f,  angle), duration * 0.5f).SetEase(Ease.InOutSine))
                   .Append(t.DOLocalRotate(new Vector3(0f, 0f, -angle), duration).SetEase(Ease.InOutSine))
                   .SetLoops(-1, LoopType.Yoyo);
                swaySequences[i] = seq;
            }

            await UniTask.WaitUntilCanceled(ct);

            foreach (var seq in swaySequences)
                seq?.Kill();

            // destroyCancellationToken キャンセル後はオブジェクト破棄済みの可能性があるため早期 return
            if (this == null) return;

            foreach (var l in _lights)
            {
                if (l.LightObject == null) continue;
                var img = l.LightObject.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.DOFade(0f, _fadeOutDuration).SetEase(Ease.InCubic);
            }

            await UniTask.Delay(
                Mathf.RoundToInt(_fadeOutDuration * 1000),
                DelayType.Realtime).SuppressCancellationThrow();
        }
    }
}
#nullable disable
