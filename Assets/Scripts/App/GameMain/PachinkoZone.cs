using System;
using UnityEngine;

namespace App
{
    /// <summary>
    /// パチンコゾーンを管理するクラス。
    /// へそ（中心穴）への入賞を検出し、GameMainController に通知する。
    /// </summary>
    public class PachinkoZone : MonoBehaviour
    {
        // ─── イベント（Observer） ────────────────────────────────
        /// <summary>へそに玉が入賞したときに発火する</summary>
        public event Action OnHesoEntered;

        // ─── 初期化 ──────────────────────────────────────────────
        /// <summary>Game2DContents.Initialize() から呼ばれる</summary>
        public void Initialize()
        {
            // TODO: へそ入賞コライダーの参照設定
        }

        // ─── 入賞検出 ────────────────────────────────────────────
        /// <summary>
        /// へそコライダーに玉が触れたときに呼ぶ。
        /// Heso オブジェクトの OnTriggerEnter2D からも呼べる。
        /// </summary>
        public void NotifyHesoEntered()
        {
            OnHesoEntered?.Invoke();
        }
    }
}
