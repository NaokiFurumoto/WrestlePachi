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
            // ─── 敵の状態 ─────────────────────────────────────────
            EditorGUILayout.LabelField("敵の状態", EditorStyles.boldLabel);
            var (idx, total, hp, maxHp) = ctrl.Debug_GetEnemyInfo();
            EditorGUILayout.LabelField($"  ステージ: {idx + 1} / {total}　HP: {hp} / {maxHp}");

            var prev2 = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button("敵を即死", GUILayout.Height(24)))
                ctrl.Debug_InstantKillEnemy();
            GUI.backgroundColor = prev2;

            EditorGUILayout.Space(8);

            // ─── 連鎖強制発動 ─────────────────────────────────────
            EditorGUILayout.LabelField("連鎖強制発動", EditorStyles.boldLabel);
            _chainCount   = EditorGUILayout.IntSlider("連鎖数",       _chainCount,   1, 10);
            _clearedCount = EditorGUILayout.IntSlider("消去ぷよ数",   _clearedCount, 1, 72);

            if (GUILayout.Button($"連鎖 {_chainCount} 回分を強制発動", GUILayout.Height(28)))
                ctrl.Debug_ForceChainCompleted(_chainCount, _clearedCount);

            EditorGUILayout.Space(8);

            // ─── ゲームオーバー / クリア ──────────────────────────
            EditorGUILayout.LabelField("その他", EditorStyles.boldLabel);
            var prev = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("ゲームクリア強制", GUILayout.Height(28)))
                ctrl.Debug_ForceGameClear();

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("ゲームオーバー強制", GUILayout.Height(28)))
                ctrl.Debug_ForceGameOver();

            GUI.backgroundColor = prev;
        }
    }
}
