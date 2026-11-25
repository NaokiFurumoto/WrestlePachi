using System;
using System.Threading;
using App.Puyo;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// 敵スキルの見た目をワンクリックで確かめるセクション（デバッグウィンドウのいちばん上）。
    /// 技のボタンを押すと、付いている技を全部外し → 技ごとの準備（盤面にぷよを置く・自動プレイなど）→
    /// その技を付けて発動 → 技に合った長さで録画、をまとめて行う。
    /// 「ゲームオーバーにしない」をオンにすると、確認中に盤面があふれても止まらない。
    /// </summary>
    public sealed class SkillVerifySection : IDebugSection
    {
        public string Title => "技の検証（ワンクリック）";
        public int    Order => -10;

        private const string PreventGameOverPrefKey = "WP_Debug_PreventGameOver";
        private const int    RecordFps              = 20;

        private int _level = 1;
        private CancellationTokenSource _cts;

        [InitializeOnLoadMethod]
        private static void ApplyPreventGameOverSetting()
            => PuyoBoard.Debug_PreventGameOver = EditorPrefs.GetBool(PreventGameOverPrefKey, true);

        /// <summary>技ごとの確認のしかた：録画する秒数と、発動の前後にする準備</summary>
        private sealed class Scenario
        {
            public EnemySkillType Type;
            public string Label = "";
            public float RecordSeconds = 6f;
            /// <summary>
            /// 発動前に盤面の下に置くぷよの行数（0なら置かない。先に盤面を空にする）。
            /// 保留の技はぷよがないと出せず天撃ボタンへストックされるので、保留を入れる技では必ず置く
            /// </summary>
            public int FillRows;
            /// <summary>自動プレイを入れる（ペアが動くのを見たい技）</summary>
            public bool AutoPlay;
            /// <summary>発動の前にする操作（保留を並べておくなど）。終わってから録画・発動する</summary>
            public Func<GameMainController, CancellationToken, UniTask> Before;
            /// <summary>発動したあとにする操作（保留を入れる・岩を叩くなど）</summary>
            public Func<GameMainController, CancellationToken, UniTask> After;
        }

        private static readonly Scenario[] Scenarios =
        {
            new() { Type = EnemySkillType.Shark,       Label = "サメ",       RecordSeconds = 7f,  FillRows = 3 },
            new() { Type = EnemySkillType.FireTile,    Label = "炎",         RecordSeconds = 7f,  FillRows = 3, AutoPlay = true },
            new() { Type = EnemySkillType.GarbageDrop, Label = "お邪魔落下", RecordSeconds = 9f,  FillRows = 3, AutoPlay = true },
            new() { Type = EnemySkillType.Rock,        Label = "岩",         RecordSeconds = 10f, FillRows = 4,
                    // 落ちてきた岩を2回叩いて、ヒビ→砕けるまで見る
                    After = async (c, ct) =>
                    {
                        await UniTask.Delay(4000, cancellationToken: ct);
                        c.Debug_HitAllRocks();
                        await UniTask.Delay(2500, cancellationToken: ct);
                        c.Debug_HitAllRocks();
                    } },
            new() { Type = EnemySkillType.TimeBomb,    Label = "時限爆弾",   RecordSeconds = 12f, FillRows = 3 },
            new() { Type = EnemySkillType.SkillSeal,   Label = "封印",       RecordSeconds = 9f,  FillRows = 3,
                    // 狙われたあとに赤と黄の保留を入れる：赤はロックされて砕け、黄は普通に技が出る
                    After = async (c, ct) =>
                    {
                        await UniTask.Delay(300, cancellationToken: ct);
                        c.Debug_AddHold(HoldType.Red);
                        await UniTask.Delay(800, cancellationToken: ct);
                        c.Debug_AddHold(HoldType.Yellow);
                    } },
            new() { Type = EnemySkillType.Confusion,      Label = "混乱",       RecordSeconds = 8f, FillRows = 2, AutoPlay = true },
            new() { Type = EnemySkillType.BlackHoldBoost, Label = "黒塗り",     RecordSeconds = 6f, FillRows = 3,
                    // 普通の保留を2つ並べてから発動：小悪魔が新しいほう（緑）を黒く塗る
                    Before = async (c, ct) =>
                    {
                        c.Debug_AddHold(HoldType.Red);
                        c.Debug_AddHold(HoldType.Green);
                        await UniTask.Delay(1300, cancellationToken: ct); // 保留玉が画面に出るまで
                    } },
            new() { Type = EnemySkillType.BlackHoldPower, Label = "黒保留威力", RecordSeconds = 7f, FillRows = 3,
                    // 黒保留を入れて、消化のときに膨らんで数が増えるのを見る
                    After = async (c, ct) => { await UniTask.Delay(300, cancellationToken: ct); c.Debug_AddHold(HoldType.Black); } },
            new() { Type = EnemySkillType.Clock,          Label = "時計",       RecordSeconds = 4f },
        };

        public void OnGUI(GameMainController ctrl)
        {
            var prevent    = EditorPrefs.GetBool(PreventGameOverPrefKey, true);
            var newPrevent = EditorGUILayout.ToggleLeft("ゲームオーバーにしない（ぷよがあふれたら盤面を空にし、時間切れなら時間を戻す）", prevent);
            if (newPrevent != prevent) EditorPrefs.SetBool(PreventGameOverPrefKey, newPrevent);
            PuyoBoard.Debug_PreventGameOver = newPrevent;

            PuyoBoard.Debug_PauseOnStuckFloating = EditorGUILayout.ToggleLeft(
                "浮いたぷよが残ったら一時停止（0.5秒以上浮いたままならコンソールに場所を出して止める）",
                PuyoBoard.Debug_PauseOnStuckFloating);

            _level = GUILayout.Toolbar(_level - 1, new[] { "Lv1", "Lv2", "Lv3" }) + 1;

            var manager = ctrl.Debug_EnemySkills;
            using (new EditorGUI.DisabledScope(manager == null || DebugFrameRecorder.IsRecording))
            {
                const int perRow = 5;
                for (var i = 0; i < Scenarios.Length; i += perRow)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (var j = i; j < Mathf.Min(i + perRow, Scenarios.Length); j++)
                        {
                            var s = Scenarios[j];
                            if (GUILayout.Button($"{s.Label}\n{s.RecordSeconds:0}秒", GUILayout.Height(36)))
                                Run(ctrl, s);
                        }
                    }
                }
            }

            if (DebugFrameRecorder.IsRecording)
                EditorGUILayout.HelpBox("録画中…終わるまで次の技は押せません", MessageType.Info);
            else if (!string.IsNullOrEmpty(DebugFrameRecorder.LastFolder))
                EditorGUILayout.LabelField("最後の保存先", DebugFrameRecorder.LastFolder, EditorStyles.miniLabel);
        }

        /// <summary>技を外す → 準備 → 録画開始 → 発動 → 発動後の操作</summary>
        private void Run(GameMainController ctrl, Scenario s)
        {
            var manager = ctrl.Debug_EnemySkills;
            if (manager == null) return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            RunAsync(ctrl, manager, s, _cts.Token).SuppressCancellationThrow().Forget();
        }

        private async UniTask RunAsync(GameMainController ctrl, EnemySkillManager manager, Scenario s, CancellationToken ct)
        {
            manager.Debug_RemoveAll();
            if (s.FillRows > 0)
            {
                ctrl.Debug_ClearBoard();
                ctrl.Debug_FillBoard(s.FillRows);
            }
            if (s.AutoPlay && !ctrl.Debug_IsAutoPlaying) ctrl.Debug_StartAutoPlay();
            if (!s.AutoPlay && ctrl.Debug_IsAutoPlaying)  ctrl.Debug_StopAutoPlay();

            if (s.Before != null) await s.Before(ctrl, ct);

            DebugFrameRecorder.Begin(s.RecordSeconds, RecordFps, $"Verify_{s.Type}_Lv{_level}");
            manager.Debug_AddAndTrigger(s.Type, _level);
            if (s.After != null) await s.After(ctrl, ct);
        }
    }
}
