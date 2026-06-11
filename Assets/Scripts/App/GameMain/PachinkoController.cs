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
        [SerializeField] private HesoTrigger   _hesoTrigger;
        [SerializeField] private PocketTrigger[] _pocketTriggers = System.Array.Empty<PocketTrigger>();

        // ─── イベント（Observer） ────────────────────────────────
        /// <summary>へそに玉が入賞したときに発火する</summary>
        public event Action OnHesoEntered;

        /// <summary>ポケットに玉が入賞したときに発火する</summary>
        public event Action OnPocketEntered;

        // ─── 初期化 ──────────────────────────────────────────────
        /// <summary>Game2DContents.Initialize() から呼ばれる</summary>
        public void Initialize()
        {
            _hesoTrigger.OnEntered += NotifyHesoEntered;
            foreach (var pocket in _pocketTriggers)
                pocket.OnEntered += NotifyPocketEntered;
        }

        private void OnDestroy()
        {
            _hesoTrigger.OnEntered -= NotifyHesoEntered;
            foreach (var pocket in _pocketTriggers)
                pocket.OnEntered -= NotifyPocketEntered;
        }

        // ─── 入賞検出 ────────────────────────────────────────────
        private void NotifyHesoEntered()  => OnHesoEntered?.Invoke();
        private void NotifyPocketEntered() => OnPocketEntered?.Invoke();
    }
}
