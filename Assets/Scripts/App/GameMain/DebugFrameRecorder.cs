#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace App
{
    /// <summary>
    /// デバッグ用：ゲーム画面を一定時間、連続で画像保存する（エディタ専用）。
    /// 演出を「実際に遊んでいるときの動き」としてコマ送りで確認するために使う。
    /// 保存先：プロジェクト直下の Temp/DebugCapture/日時_ラベル/frame_000.jpg …
    /// （Temp はエディタ終了時に消える・git 管理外）
    /// </summary>
    public sealed class DebugFrameRecorder : MonoBehaviour
    {
        /// <summary>録画中か</summary>
        public static bool IsRecording { get; private set; }

        /// <summary>最後に保存したフォルダ（まだ無ければ空文字）</summary>
        public static string LastFolder { get; private set; } = "";

        /// <summary>
        /// 録画を始める。録画中なら何もしない。
        /// </summary>
        /// <param name="seconds">録画する秒数</param>
        /// <param name="fps">1秒あたりの保存枚数</param>
        /// <param name="label">フォルダ名に付ける説明（技名など）</param>
        public static void Begin(float seconds, int fps, string label)
        {
            if (IsRecording || !Application.isPlaying) return;
            var go = new GameObject("DebugFrameRecorder");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugFrameRecorder>().StartCoroutine(RecordRoutine(go, seconds, fps, label));
        }

        private static IEnumerator RecordRoutine(GameObject owner, float seconds, int fps, string label)
        {
            IsRecording = true;
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "DebugCapture",
                                      $"{DateTime.Now:yyyyMMdd_HHmmss}_{label}");
            Directory.CreateDirectory(folder);

            var total    = Mathf.Max(1, Mathf.RoundToInt(seconds * fps));
            var interval = 1f / Mathf.Max(1, fps);
            var start    = Time.realtimeSinceStartup;
            var times    = new System.Text.StringBuilder();

            for (var i = 0; i < total; i++)
            {
                // 実時間で一定間隔になるよう待ってから、描画し終わったフレームを撮る
                var due = start + i * interval;
                while (Time.realtimeSinceStartup < due) yield return null;
                yield return new WaitForEndOfFrame();

                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder, $"frame_{i:000}.jpg"), tex.EncodeToJPG(80));
                Destroy(tex);
                times.AppendLine($"{i}\t{Time.realtimeSinceStartup - start:0.000}");
            }

            File.WriteAllText(Path.Combine(folder, "times.txt"), times.ToString());
            LastFolder  = folder;
            IsRecording = false;
            Debug.Log($"[DebugFrameRecorder] {total}枚を保存しました: {folder}");
            Destroy(owner);
        }

        private void OnDestroy() => IsRecording = false;
    }
}
#nullable disable
#endif
