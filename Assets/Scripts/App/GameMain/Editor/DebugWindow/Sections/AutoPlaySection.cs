using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// 自動プレイのデバッグセクション。
    /// ON にすると CPU がぷよを自動操作する。
    /// </summary>
    public sealed class AutoPlaySection : IDebugSection
    {
        public string Title => "自動プレイ";
        public int    Order => 10;

        private int _thinkMs = 300;
        private int _moveMs  = 100;

        public void OnGUI(GameMainController ctrl)
        {
            var isOn = ctrl.Debug_IsAutoPlaying;

            // ─── ON/OFF トグルボタン ──────────────────────────────
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = isOn
                ? new Color(0.3f, 0.9f, 0.3f)
                : new Color(0.65f, 0.65f, 0.65f);

            if (GUILayout.Button(
                    isOn ? "■  AUTO PLAY  停止" : "▶  AUTO PLAY  開始",
                    GUILayout.Height(38)))
            {
                if (isOn) ctrl.Debug_StopAutoPlay();
                else      ctrl.Debug_StartAutoPlay(_thinkMs, _moveMs);
            }
            GUI.backgroundColor = prev;

            EditorGUILayout.Space(4);

            // ─── 速度設定（停止中のみ変更可能）──────────────────────
            using (new EditorGUI.DisabledGroupScope(isOn))
            {
                _thinkMs = EditorGUILayout.IntSlider("思考遅延 (ms)", _thinkMs, 0,   1000);
                _moveMs  = EditorGUILayout.IntSlider("移動遅延 (ms)", _moveMs,  30,  300);
            }

            if (isOn)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "CPU が自動でぷよを操作中。速度を変えるには一度停止してください。",
                    MessageType.Info);
            }
        }
    }
}
