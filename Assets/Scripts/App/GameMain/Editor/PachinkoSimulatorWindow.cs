using App.Simulator;
using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// パチンコシミュレーター専用デバッグウィンドウ。
    ///
    /// 【手動シミュレーション】指定数の玉を自動発射して入賞率を計測する。
    /// 【自動釘調整】ヒルクライミング法で釘位置を調整し、入賞率を目標に近づける。
    ///
    /// メニュー: WrestlePachi > パチンコシミュレーター
    /// </summary>
    public sealed class PachinkoSimulatorWindow : EditorWindow
    {
        // ─── メニュー ─────────────────────────────────────────────

        [MenuItem("WrestlePachi/パチンコシミュレーター")]
        private static void Open() => GetWindow<PachinkoSimulatorWindow>("パチンコシミュレーター");

        // ─── UI 状態 ─────────────────────────────────────────────
        private int    _ballCount      = 125;
        private float  _timeScale      = 2f;
        private string _resultMessage  = "";
        private bool   _resultPass;
        private bool   _wasSimRunning;
        private bool   _wasAdjRunning;

        // 目標設定
        private const int   TargetBalls = 125;
        private const int   TargetEntry = 10;
        private const float TargetRate  = (float)TargetEntry / TargetBalls;
        private const float PassMargin  = 0.015f;

        // シーン上のコンポーネントキャッシュ
        private PachinkoSimulator _simulator;
        private NailAutoAdjuster  _adjuster;

        // ─── GUIスタイル ──────────────────────────────────────────
        private GUIStyle _sectionLabel;
        private GUIStyle _statLabel;
        private GUIStyle _resultPass_Style;
        private GUIStyle _resultFail_Style;
        private GUIStyle _statusLabel;

        private void InitStyles()
        {
            if (_sectionLabel != null) return;

            _sectionLabel = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            _statLabel    = new GUIStyle(EditorStyles.label)     { fontSize = 14 };

            _resultPass_Style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 20,
                alignment = TextAnchor.MiddleCenter,
                wordWrap  = true,
                normal    = { textColor = new Color(0.2f, 0.9f, 0.3f) },
            };
            _resultFail_Style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 20,
                alignment = TextAnchor.MiddleCenter,
                wordWrap  = true,
                normal    = { textColor = new Color(1f, 0.3f, 0.3f) },
            };
            _statusLabel = new GUIStyle(EditorStyles.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal   = { textColor = new Color(0.8f, 0.85f, 1f) },
            };
        }

        // ─── ライフサイクル ───────────────────────────────────────

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                // PlayMode終了時に採用済み釘位置をUndo対応でシーンに反映する
                ApplyPendingAdjustments();
            }

            _simulator     = null;
            _adjuster      = null;
            _resultMessage = "";
            _wasSimRunning = false;
            _wasAdjRunning = false;
            Repaint();
        }

        // ─── GUI ──────────────────────────────────────────────────

        private void OnGUI()
        {
            InitStyles();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play モード中のみ使用できます", MessageType.Info);

                // 前回の調整結果が残っていれば適用ボタンを表示
                if (NailAutoAdjuster.PendingAdjustments.Count > 0)
                {
                    EditorGUILayout.Space(8);
                    EditorGUILayout.HelpBox(
                        $"未適用の釘調整が {NailAutoAdjuster.PendingAdjustments.Count} 件あります。",
                        MessageType.Warning);
                    if (GUILayout.Button("シーンに適用（Undo 対応）", GUILayout.Height(34)))
                        ApplyPendingAdjustments();
                }
                return;
            }

            // シーンからコンポーネントを取得
            if (_simulator == null) _simulator = FindObjectOfType<PachinkoSimulator>();
            if (_adjuster  == null) _adjuster  = FindObjectOfType<NailAutoAdjuster>();

            if (_simulator == null)
            {
                EditorGUILayout.HelpBox(
                    "PachinkoSimulator コンポーネントがシーンに見つかりません。",
                    MessageType.Warning);
                return;
            }

            // 完了検出
            if (_wasSimRunning && !_simulator.IsRunning) BuildSimResultMessage();
            _wasSimRunning = _simulator.IsRunning;

            if (_adjuster != null)
            {
                if (_wasAdjRunning && !_adjuster.IsAdjusting) BuildAdjResultMessage();
                _wasAdjRunning = _adjuster.IsAdjusting;
            }

            EditorGUILayout.Space(10);

            // ──────────────────────────────────────────────────────
            // セクション 1: 手動シミュレーション
            // ──────────────────────────────────────────────────────
            EditorGUILayout.LabelField("手動シミュレーション", _sectionLabel);

            if (!_simulator.IsRunning && (_adjuster == null || !_adjuster.IsAdjusting))
            {
                DrawSettings();
                EditorGUILayout.Space(8);
                DrawStartButton();
            }
            else if (_simulator.IsRunning)
            {
                DrawRunningStatus();
                EditorGUILayout.Space(8);
                DrawStopButton();
            }

            DrawResult();

            // ──────────────────────────────────────────────────────
            // セクション 2: 自動釘調整
            // ──────────────────────────────────────────────────────
            if (_adjuster != null)
            {
                EditorGUILayout.Space(16);
                DrawSeparator();
                EditorGUILayout.Space(8);
                DrawAutoAdjustSection();
            }
            else
            {
                EditorGUILayout.Space(12);
                EditorGUILayout.HelpBox(
                    "NailAutoAdjuster コンポーネントをシーン上のGameObjectに追加すると自動釘調整が使えます。",
                    MessageType.Info);
            }

            Repaint();
        }

        // ─── 手動シミュレーション UI ─────────────────────────────

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _ballCount = EditorGUILayout.IntSlider("発射数", _ballCount, 10, 1000);
            _timeScale = EditorGUILayout.Slider("速度倍率", _timeScale, 1f, 8f);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(
                $"目標: {TargetBalls}発で{TargetEntry}入賞 ({TargetRate * 100f:F1}%)",
                _statLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawStartButton()
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
            if (GUILayout.Button("▶  シミュレーション開始", GUILayout.Height(40)))
            {
                _resultMessage = "";
                _simulator.StartSimulation(_ballCount, _timeScale);
            }
            GUI.backgroundColor = prev;
        }

        private void DrawRunningStatus()
        {
            EditorGUILayout.LabelField("実行中...", _sectionLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                $"発射済み:  {_simulator.LaunchCount} / {_simulator.TargetCount}", _statLabel);
            EditorGUILayout.LabelField(
                $"入賞数:    {_simulator.EntryCount}  ({_simulator.EntryRate * 100f:F1}%)", _statLabel);
            EditorGUILayout.Space(4);
            var progress = _simulator.TargetCount > 0
                ? (float)_simulator.LaunchCount / _simulator.TargetCount : 0f;
            var rect = GUILayoutUtility.GetRect(18, 22, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, progress,
                $"{_simulator.LaunchCount} / {_simulator.TargetCount}");
            EditorGUILayout.EndVertical();
        }

        private void DrawStopButton()
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("■  停止・リセット", GUILayout.Height(40)))
                _simulator.StopSimulation();
            GUI.backgroundColor = prev;
        }

        private void DrawResult()
        {
            if (string.IsNullOrEmpty(_resultMessage)) return;
            EditorGUILayout.Space(12);
            var style = _resultPass ? _resultPass_Style : _resultFail_Style;
            var rect  = GUILayoutUtility.GetRect(
                new GUIContent(_resultMessage), style,
                GUILayout.ExpandWidth(true), GUILayout.MinHeight(60));
            GUI.Label(rect, _resultMessage, style);
        }

        private void BuildSimResultMessage()
        {
            var rate = _simulator.EntryRate;
            var diff = rate - TargetRate;
            _resultPass = Mathf.Abs(diff) <= PassMargin;

            string judge = _resultPass         ? "★ 合格！"
                         : diff > 0f           ? "▲ 入賞しすぎ"
                                               : "▼ 入賞が少ない";

            _resultMessage =
                $"{_simulator.LaunchCount}発 → {_simulator.EntryCount}入賞 ({rate * 100f:F1}%)\n" +
                $"目標 {TargetRate * 100f:F1}%  {judge}";
        }

        // ─── 自動釘調整 UI ───────────────────────────────────────

        private void DrawAutoAdjustSection()
        {
            EditorGUILayout.LabelField("自動釘調整", _sectionLabel);

            if (_adjuster.IsAdjusting)
            {
                DrawAdjustingStatus();
                EditorGUILayout.Space(8);
                DrawStopAdjustButton();
            }
            else if (!_simulator.IsRunning)
            {
                DrawAdjustInfo();
                EditorGUILayout.Space(8);
                DrawStartAdjustButton();
            }
        }

        private void DrawAdjustInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            var nailCount = 0;
            // NailAutoAdjusterのフィールドはprivateなので表示用に概要のみ
            EditorGUILayout.LabelField(
                $"試行回数: {_adjuster.MaxTrials} 回  /  各試行: {125}発・2x速", _statLabel);
            EditorGUILayout.LabelField(
                $"現在の入賞率: {_adjuster.LastRate * 100f:F1}%  （目標 8.0%）", _statLabel);
            if (NailAutoAdjuster.PendingAdjustments.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(
                    $"採用済み変更: {NailAutoAdjuster.PendingAdjustments.Count} 件（Play終了時に自動反映）",
                    _statLabel);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawAdjustingStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var progress = _adjuster.MaxTrials > 0
                ? (float)_adjuster.CurrentTrial / _adjuster.MaxTrials : 0f;
            var rect = GUILayoutUtility.GetRect(18, 22, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, progress,
                $"試行 {_adjuster.CurrentTrial} / {_adjuster.MaxTrials}");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(_adjuster.StatusText, _statusLabel);
            EditorGUILayout.LabelField(
                $"採用済み変更: {NailAutoAdjuster.PendingAdjustments.Count} 件", _statLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawStartAdjustButton()
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button("⚙  自動釘調整 開始", GUILayout.Height(40)))
            {
                _resultMessage = "";
                _adjuster.StartAdjustment();
            }
            GUI.backgroundColor = prev;
        }

        private void DrawStopAdjustButton()
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("■  調整を中断", GUILayout.Height(40)))
                _adjuster.StopAdjustment();
            GUI.backgroundColor = prev;
        }

        private void BuildAdjResultMessage()
        {
            var rate = _adjuster.LastRate;
            _resultPass = Mathf.Abs(rate - TargetRate) <= PassMargin;

            string judge = _resultPass  ? "★ 合格！"
                         : rate > TargetRate + PassMargin ? "▲ 入賞しすぎ"
                                                          : "▼ 入賞が少ない";

            _resultMessage =
                $"調整完了: {rate * 100f:F1}%  {judge}\n" +
                $"採用変更 {NailAutoAdjuster.PendingAdjustments.Count} 件 → Play終了時にシーンへ反映";
        }

        // ─── 分割線 ───────────────────────────────────────────────

        private static void DrawSeparator()
        {
            var rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        // ─── Playモード終了後のUndo対応シーン反映 ─────────────────

        private static void ApplyPendingAdjustments()
        {
            var pending = NailAutoAdjuster.PendingAdjustments;
            if (pending.Count == 0) return;

            // シーン上の全Transformをhierarchyパスで辞書化
            var allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            var pathMap = new System.Collections.Generic.Dictionary<string, Transform>();
            foreach (var t in allTransforms)
            {
                var path = GetHierarchyPath(t);
                pathMap.TryAdd(path, t);
            }

            Undo.SetCurrentGroupName("釘位置自動調整");
            int group = Undo.GetCurrentGroup();

            int applied = 0;
            foreach (var (path, pos) in pending)
            {
                if (!pathMap.TryGetValue(path, out var nail)) continue;
                Undo.RecordObject(nail, "釘位置調整");
                nail.position = pos;
                EditorUtility.SetDirty(nail);
                applied++;
            }

            Undo.CollapseUndoOperations(group);
            pending.Clear();

            if (applied > 0)
                Debug.Log($"[NailAutoAdjuster] {applied} 本の釘位置をシーンに反映しました（Ctrl+Z で元に戻せます）");
        }

        private static string GetHierarchyPath(Transform t)
        {
            var path = t.name;
            var p    = t.parent;
            while (p != null) { path = p.name + "/" + path; p = p.parent; }
            return path;
        }
    }
}
