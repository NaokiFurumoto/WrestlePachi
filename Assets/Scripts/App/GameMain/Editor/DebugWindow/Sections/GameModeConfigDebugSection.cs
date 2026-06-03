using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// GameModeConfig のデバッグセクション。
    /// Play 中にパラメーターをプリセットボタンで即切り替えする。
    /// </summary>
    public sealed class GameModeConfigDebugSection : IDebugSection
    {
        public string Title => "GameModeConfig";
        public int    Order => 3;

        public void OnGUI(GameMainController ctrl)
        {
            var config = ctrl.Debug_GameModeConfig;
            if (config == null)
            {
                EditorGUILayout.HelpBox("GameModeConfig が null です", MessageType.Warning);
                return;
            }

            // ─── Black 保留確率 ────────────────────────────────────
            EditorGUILayout.LabelField(
                $"Black 保留確率（現在: {config.BlackHoldProbability:P0}）",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("0%"))    ctrl.Debug_SetBlackProbability(0f);
                if (GUILayout.Button("10%"))   ctrl.Debug_SetBlackProbability(0.1f);
                if (GUILayout.Button("50%"))   ctrl.Debug_SetBlackProbability(0.5f);
                if (GUILayout.Button("100%"))  ctrl.Debug_SetBlackProbability(1f);
            }

            EditorGUILayout.Space(6);

            // ─── BallsPerPuyo ──────────────────────────────────────
            EditorGUILayout.LabelField(
                $"BallsPerPuyo（現在: {config.Debug_BallsPerPuyo:F1}）",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Easy (0.5)"))    ctrl.Debug_SetBallsPerPuyo(0.5f);
                if (GUILayout.Button("Normal (1.0)"))  ctrl.Debug_SetBallsPerPuyo(1.0f);
                if (GUILayout.Button("Hard (2.0)"))    ctrl.Debug_SetBallsPerPuyo(2.0f);
            }

            EditorGUILayout.Space(6);

            // ─── 色バリアント ──────────────────────────────────────
            EditorGUILayout.LabelField(
                $"保留 / ぷよ 種類数（現在: {config.ColorVariant} 種）",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = config.ColorVariant == 4 ? Color.cyan : Color.white;
                if (GUILayout.Button("4種  Red〜Blue"))   ctrl.Debug_SetColorVariant(4);
                GUI.backgroundColor = config.ColorVariant == 5 ? Color.cyan : Color.white;
                if (GUILayout.Button("5種  Red〜Purple")) ctrl.Debug_SetColorVariant(5);
                GUI.backgroundColor = Color.white;
            }
        }
    }
}
