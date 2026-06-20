using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    public sealed class ComboDebugSection : IDebugSection
    {
        public string Title => "コンボ演出";
        public int    Order => 15;

        private int    _chainCount   = 2;
        private bool   _isAutoPlaying;
        private int    _autoChain;
        private double _nextTime;
        private GameMainController _ctrl;

        public void OnGUI(GameMainController ctrl)
        {
            _ctrl = ctrl;

            // 手動
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("連鎖数", GUILayout.Width(48));
                _chainCount = EditorGUILayout.IntSlider(_chainCount, 2, 10);
            }

            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button($"{_chainCount} 連鎖コンボを表示", GUILayout.Height(28)))
                ctrl.Debug_ShowCombo(_chainCount);
            GUI.backgroundColor = prev;

            EditorGUILayout.Space(4);

            // 連続再生
            var wasAuto = _isAutoPlaying;
            _isAutoPlaying = GUILayout.Toggle(_isAutoPlaying, "連続再生（2→10連鎖を順番に）");
            if (_isAutoPlaying && !wasAuto)
            {
                _autoChain = 2;
                _nextTime  = EditorApplication.timeSinceStartup;
                EditorApplication.update += AutoUpdate;
            }
            else if (!_isAutoPlaying && wasAuto)
            {
                EditorApplication.update -= AutoUpdate;
            }

            if (_isAutoPlaying)
                EditorGUILayout.LabelField($"次: {_autoChain} 連鎖", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("スケール目安", EditorStyles.miniLabel);
            for (var i = 2; i <= 6; i++)
            {
                var scale = Mathf.Min(1.3f + i * 0.15f, 2.2f);
                EditorGUILayout.LabelField($"  {i} 連鎖 → ×{scale:F2}", EditorStyles.miniLabel);
            }
        }

        private void AutoUpdate()
        {
            if (!_isAutoPlaying || _ctrl == null || !Application.isPlaying)
            {
                _isAutoPlaying = false;
                EditorApplication.update -= AutoUpdate;
                return;
            }

            if (EditorApplication.timeSinceStartup < _nextTime) return;

            _ctrl.Debug_ShowCombo(_autoChain);
            _autoChain++;
            if (_autoChain > 10) _autoChain = 2;
            _nextTime = EditorApplication.timeSinceStartup + 1.5;
        }
    }
}
