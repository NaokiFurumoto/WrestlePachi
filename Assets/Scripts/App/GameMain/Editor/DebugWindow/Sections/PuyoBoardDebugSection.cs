using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// ぷよボードのデバッグセクション。
    /// </summary>
    public sealed class PuyoBoardDebugSection : IDebugSection
    {
        public string Title => "ぷよボード";
        public int    Order => 10;

        private int _fillRows = 6;

        public void OnGUI(GameMainController ctrl)
        {
            EditorGUILayout.LabelField("盤面操作", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("行数", GUILayout.Width(40));
                _fillRows = EditorGUILayout.IntSlider(_fillRows, 1, 10);
            }

            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button($"ぷよをランダム配置（下 {_fillRows} 行）", GUILayout.Height(32)))
                ctrl.Debug_FillBoard(_fillRows);
            GUI.backgroundColor = prev;
        }
    }
}
