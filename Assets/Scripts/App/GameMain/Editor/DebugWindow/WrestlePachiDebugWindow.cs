using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// WrestlePachi デバッグウィンドウ。
    /// IDebugSection を実装したクラスを TypeCache で自動収集して一覧表示する。
    /// 新しいセクションを追加するには IDebugSection を実装するだけでよい。
    ///
    /// メニュー: WrestlePachi > デバッグウィンドウ  (Ctrl+Shift+D)
    /// </summary>
    public sealed class WrestlePachiDebugWindow : EditorWindow
    {
        // ─── 状態 ────────────────────────────────────────────────
        private List<IDebugSection> _sections;
        private bool[]              _foldouts;
        private Vector2             _scroll;

        // ─── メニュー ─────────────────────────────────────────────

        [MenuItem("WrestlePachi/デバッグウィンドウ %#d")]
        private static void Open() => GetWindow<WrestlePachiDebugWindow>("WP Debug");

        // ─── ライフサイクル ───────────────────────────────────────

        private void OnEnable()
        {
            // IDebugSection を実装した全具象クラスを自動収集・Order 順にソート
            _sections = TypeCache.GetTypesDerivedFrom<IDebugSection>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Select(t => (IDebugSection)System.Activator.CreateInstance(t))
                .OrderBy(s => s.Order)
                .ToList();

            _foldouts = new bool[_sections.Count];
            for (var i = 0; i < _foldouts.Length; i++) _foldouts[i] = true;

            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange _) => Repaint();

        // ─── GUI ──────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play モード中のみ使用できます", MessageType.Info);
                return;
            }

            var ctrl = FindObjectOfType<GameMainController>();
            if (ctrl == null)
            {
                EditorGUILayout.HelpBox("GameMainController が見つかりません", MessageType.Warning);
                return;
            }

            DrawHeader(ctrl);
            DrawSections(ctrl);

            Repaint(); // Play 中はリアルタイム更新
        }

        private static void DrawHeader(GameMainController ctrl)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField(
                    $"ステート: {ctrl.Debug_CurrentPhase}",
                    EditorStyles.boldLabel);
            }
            EditorGUILayout.Space(4);
        }

        private void DrawSections(GameMainController ctrl)
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (var i = 0; i < _sections.Count; i++)
            {
                _foldouts[i] = EditorGUILayout.Foldout(
                    _foldouts[i], _sections[i].Title, true, EditorStyles.foldoutHeader);

                if (_foldouts[i])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    _sections[i].OnGUI(ctrl);
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
