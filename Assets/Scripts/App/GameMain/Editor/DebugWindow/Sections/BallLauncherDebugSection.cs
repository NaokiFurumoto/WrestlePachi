using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// 玉発射のデバッグセクション。
    /// 任意球数の発射とシーン上の全玉クリアを提供する。
    /// </summary>
    public sealed class BallLauncherDebugSection : IDebugSection
    {
        public string Title => "玉発射";
        public int    Order => 1;

        private int _ballCount = 5;

        public void OnGUI(GameMainController ctrl)
        {
            _ballCount = EditorGUILayout.IntSlider("発射球数", _ballCount, 1, 30);

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("発射", GUILayout.Height(28)))
                    ctrl.Debug_LaunchBalls(_ballCount);

                var prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("全玉クリア", GUILayout.Height(28)))
                    ctrl.Debug_ClearAllBalls();
                GUI.backgroundColor = prev;
            }
        }
    }
}
