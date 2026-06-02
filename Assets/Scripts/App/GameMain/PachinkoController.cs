using System;
using UnityEngine;

namespace App
{
    /// <summary>
    /// パチンコ台全体を管理するクラス。
    /// HesoTrigger を購読してへそ入賞を GameMainController に通知する。
    /// </summary>
    public class PachinkoController : MonoBehaviour
    {
        // ─── Inspector 参照 ───────────────────────────────────────
        [SerializeField] private HesoTrigger _hesoTrigger;

        // ─── イベント（Observer） ────────────────────────────────
        /// <summary>へそに玉が入賞したときに発火する</summary>
        public event Action OnHesoEntered;

        // ─── 初期化 ──────────────────────────────────────────────
        /// <summary>Game2DContents.Initialize() から呼ばれる</summary>
        public void Initialize()
        {
            _hesoTrigger.OnEntered += NotifyHesoEntered;
        }

        private void OnDestroy()
        {
            _hesoTrigger.OnEntered -= NotifyHesoEntered;
        }

        // ─── 入賞検出 ────────────────────────────────────────────
        private void NotifyHesoEntered()
        {
            OnHesoEntered?.Invoke();
        }
    }
}
