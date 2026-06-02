using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// ゲームフローのデバッグセクション。
    /// 連鎖強制発動・ゲームオーバー強制を提供する。
    /// </summary>
    public sealed class GameFlowDebugSection : IDebugSection
    {
        public string Title => "ゲームフロー";
        public int    Order => 2;

        private int _chainCount   = 3;
        private int _clearedCount = 12;

        public void OnGUI(GameMainController ctrl)
        {
            // ─── 連鎖強制発動 ─────────────────────────────────────
            EditorGUILayout.LabelField("連鎖強制発動", EditorStyles.boldLabel);
            _chainCount   = EditorGUILayout.IntSlider("連鎖数",       _chainCount,   1, 10);
            _clearedCount = EditorGUILayout.IntSlider("消去ぷよ数",   _clearedCount, 1, 72);

            if (GUILayout.Button($"連鎖 {_chainCount} 回分を強制発動", GUILayout.Height(28)))
                ctrl.Debug_ForceChainCompleted(_chainCount, _clearedCount);

            EditorGUILayout.Space(8);

            // ─── ゲームオーバー ────────────────────────────────────
            EditorGUILayout.LabelField("その他", EditorStyles.boldLabel);
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("ゲームオーバー強制", GUILayout.Height(28)))
                ctrl.Debug_ForceGameOver();
            GUI.backgroundColor = prev;
        }
    }
}
