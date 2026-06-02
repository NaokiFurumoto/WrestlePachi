using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// 保留システムのデバッグセクション。
    /// スロット状態の確認・種別指定での保留追加・強制へそ入賞を提供する。
    /// </summary>
    public sealed class HoldDebugSection : IDebugSection
    {
        public string Title => "保留システム";
        public int    Order => 0;

        private static readonly (string label, HoldType type, Color color)[] s_buttons =
        {
            ("赤",   HoldType.Red,    new Color(1f,   0.4f, 0.4f)),
            ("黄",   HoldType.Yellow, new Color(1f,   0.9f, 0.2f)),
            ("緑",   HoldType.Green,  new Color(0.3f, 0.9f, 0.3f)),
            ("青",   HoldType.Blue,   new Color(0.3f, 0.6f, 1f  )),
            ("黒",   HoldType.Black,  new Color(0.5f, 0.5f, 0.5f)),
        };

        public void OnGUI(GameMainController ctrl)
        {
            // ─── スロット状態 ─────────────────────────────────────
            EditorGUILayout.LabelField("スロット状態", EditorStyles.boldLabel);
            var slots = ctrl.Debug_GetHoldSlots();

            using (new EditorGUILayout.HorizontalScope())
            {
                for (var i = 0; i < slots.Length; i++)
                {
                    var slotLabel = slots[i]?.ToString() ?? "─";
                    EditorGUILayout.LabelField(
                        $"slot{i + 1}: {slotLabel}",
                        GUILayout.Width(100));
                }
            }

            EditorGUILayout.Space(6);

            // ─── 保留追加 ─────────────────────────────────────────
            EditorGUILayout.LabelField("保留追加（種別指定）", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var (label, type, color) in s_buttons)
                {
                    var prev = GUI.backgroundColor;
                    GUI.backgroundColor = color;
                    if (GUILayout.Button(label, GUILayout.Height(28)))
                        ctrl.Debug_AddHold(type);
                    GUI.backgroundColor = prev;
                }
            }

            EditorGUILayout.Space(4);

            // ─── 強制へそ入賞 ─────────────────────────────────────
            if (GUILayout.Button("強制へそ入賞（ランダム種別）", GUILayout.Height(28)))
                ctrl.Debug_ForceHesoEntry();
        }
    }
}
