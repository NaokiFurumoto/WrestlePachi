#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 保留消化時に発射元から目標へ向かうレーザービーム演出。
    /// 先端が細く尾端が太いトレイル形状で LineRenderer を使用。
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class HoldBeamEffect : MonoBehaviour
    {
        [SerializeField, Tooltip("ビームの移動時間（秒）")]
        private float _duration = 0.25f;

        [SerializeField, Tooltip("尾端の幅")]
        private float _tailWidth = 0.25f;

        [SerializeField, Tooltip("先端の幅")]
        private float _headWidth = 0.02f;

        private LineRenderer _lr = null!;

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.positionCount = 2;
            _lr.useWorldSpace = true;
            _lr.sortingOrder  = 100;
            _lr.enabled = false;

            // 尾端（position[0]）=太、先端（position[1]）=細
            var curve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f)
            );
            _lr.widthCurve = curve;
        }

        /// <summary>from から to へビームを走らせる。</summary>
        public async UniTaskVoid PlayAsync(Vector3 from, Vector3 to, Color color, CancellationToken ct)
        {
            // 前の再生を上書き
            _lr.startColor     = color;
            _lr.endColor       = new Color(color.r, color.g, color.b, 0f);
            _lr.widthMultiplier = _tailWidth;
            _lr.SetPosition(0, from);
            _lr.SetPosition(1, from);
            _lr.enabled = true;

            float elapsed = 0f;
            while (elapsed < _duration)
            {
                if (ct.IsCancellationRequested) break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);

                // 先端が先行し、尾端が 40% 遅れて追いかける
                float headT = Mathf.Min(t / 0.6f, 1f);
                float tailT = Mathf.Max((t - 0.4f) / 0.6f, 0f);

                _lr.SetPosition(1, Vector3.Lerp(from, to, headT));
                _lr.SetPosition(0, Vector3.Lerp(from, to, tailT));

                await UniTask.NextFrame(cancellationToken: CancellationToken.None);
            }

            _lr.enabled = false;
        }
    }
}
#nullable disable
