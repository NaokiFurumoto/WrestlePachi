using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// 敵スキルのデバッグセクション。
    /// 発動中の技の一覧（次の発動までの秒数）・今すぐ発動・外す、
    /// 好きな技とLvをその場で付けてすぐ発動する操作を提供する。
    /// </summary>
    public sealed class EnemySkillDebugSection : IDebugSection
    {
        public string Title => "敵スキル";
        public int    Order => 3;

        private static readonly EnemySkillType[] AllTypes =
            (EnemySkillType[])Enum.GetValues(typeof(EnemySkillType));

        private static readonly string[] TypeLabels =
            AllTypes.Select(EnemySkillNames.Get).ToArray();

        private int _typeIndex;
        private int _level = 1;

        public void OnGUI(GameMainController ctrl)
        {
            var manager = ctrl.Debug_EnemySkills;
            if (manager == null)
            {
                EditorGUILayout.HelpBox("敵スキルがまだ初期化されていません", MessageType.Info);
                return;
            }

            DrawActiveSkills(manager);
            EditorGUILayout.Space(8);
            DrawAddSkill(manager);
        }

        // ─── 発動中の技 ───────────────────────────────────────────
        private static void DrawActiveSkills(EnemySkillManager manager)
        {
            EditorGUILayout.LabelField("発動中の技", EditorStyles.boldLabel);

            var skills = manager.Debug_Skills;
            if (skills.Count == 0)
            {
                EditorGUILayout.LabelField("  （なし）");
                return;
            }

            // 一覧の描画中に外すとリストが変わるので、押されたものは描画後に処理する
            IEnemySkill toRemove = null;
            foreach (var skill in skills)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{EnemySkillNames.Get(skill.Type)} Lv{skill.Level}", GUILayout.Width(140));

                    if (skill is EnemySkillBase periodic)
                    {
                        EditorGUILayout.LabelField($"次の発動まで {periodic.SecondsUntilNext:0.0}秒", GUILayout.Width(130));
                        if (GUILayout.Button("今すぐ発動", GUILayout.Width(80)))
                            manager.Debug_TriggerNow(periodic);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("（常に有効）", GUILayout.Width(214));
                    }

                    if (GUILayout.Button("外す", GUILayout.Width(50)))
                        toRemove = skill;
                }
            }

            if (toRemove != null) manager.Debug_Remove(toRemove);

            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("すべての技を外す", GUILayout.Height(22)))
                manager.Debug_RemoveAll();
            GUI.backgroundColor = prev;
        }

        // ─── 技を追加 ─────────────────────────────────────────────
        private void DrawAddSkill(EnemySkillManager manager)
        {
            EditorGUILayout.LabelField("技を追加", EditorStyles.boldLabel);
            _typeIndex = EditorGUILayout.Popup("技", _typeIndex, TypeLabels);
            _level     = EditorGUILayout.IntSlider("Lv", _level, 1, EnemySkillRule.MaxSkillLevel);

            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button("追加して発動", GUILayout.Height(28)))
                manager.Debug_AddAndTrigger(AllTypes[_typeIndex], _level);
            GUI.backgroundColor = prev;
        }
    }
}
