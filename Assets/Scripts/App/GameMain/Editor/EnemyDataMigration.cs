#if UNITY_EDITOR
#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using static App.EnemySkillType;

namespace App.EditorTools
{
    /// <summary>
    /// 1回限りの移行メニュー：StageConfig の旧フィールド（敵名・顔画像）から EnemyData アセットを作り、
    /// 各ステージの Enemy / IsRematch を設定する。あわせて敵ごとの得意技・2戦目の追加技を割り当てる。
    /// 何度実行しても同じ結果になる（既存アセットは作り直さず中身だけ上書き）。
    /// 移行が済んだら旧フィールドと一緒にこのファイルを削除する。
    /// </summary>
    public static class EnemyDataMigration
    {
        private const string StageListPath = "Assets/Resources/StageConfigList.asset";
        private const string EnemyFolder   = "Assets/Settings/Enemies";
        private const string FaceSheetPath = "Assets/Textures/GameMain/Puyo/Enemys01.png";

        /// <summary>敵1体分の移行設定（アセット名・得意技・2戦目の追加技・顔の上書き）</summary>
        private sealed class Plan
        {
            public string            AssetName = "";
            public EnemySkillEntry[] Signature = System.Array.Empty<EnemySkillEntry>();
            public EnemySkillEntry[] Extra     = System.Array.Empty<EnemySkillEntry>();
            public string?           FaceOverride;
        }

        private static EnemySkillEntry S(EnemySkillType type, int level) => new() { Type = type, Level = level };

        // 設計書 §6.4 / §6.5 の割り当て表（旧 EnemyName → 設定）
        private static readonly Dictionary<string, Plan> Plans = new()
        {
            ["へなちょこマスク"]     = new() { AssetName = "Enemy_Henachoko" },
            ["クロマスク"]           = new() { AssetName = "Enemy_KuroMask",        Extra = new[] { S(GarbageDrop, 1) } },
            ["アニマスク"]           = new() { AssetName = "Enemy_AniMask",         Signature = new[] { S(Clock, 1) },     Extra = new[] { S(GarbageDrop, 1) } },
            ["デビルマスク"]         = new() { AssetName = "Enemy_DevilMask",       Signature = new[] { S(FireTile, 1) },  Extra = new[] { S(Clock, 1) } },
            ["パインマスク"]         = new() { AssetName = "Enemy_PineMask",        Signature = new[] { S(TimeBomb, 1) },  Extra = new[] { S(GarbageDrop, 1) } },
            ["マッハキラー"]         = new() { AssetName = "Enemy_MachKiller",      Signature = new[] { S(Clock, 2), S(GarbageDrop, 1) } },
            ["スコーピオンX"]        = new() { AssetName = "Enemy_ScorpionX",       Signature = new[] { S(SkillSeal, 1) }, Extra = new[] { S(BlackHoldBoost, 1) } },
            ["サンダーボルト"]       = new() { AssetName = "Enemy_Thunderbolt",     Signature = new[] { S(Confusion, 1) }, Extra = new[] { S(BlackHoldPower, 1) } },
            ["デスドライバー"]       = new() { AssetName = "Enemy_DeathDriver",     Signature = new[] { S(TimeBomb, 2), S(Clock, 2) } },
            ["バジリスク王"]         = new() { AssetName = "Enemy_BasiliskKing",    Signature = new[] { S(Rock, 1), S(SkillSeal, 1) },     Extra = new[] { S(BlackHoldBoost, 1) }, FaceOverride = "Enemys01_7" },
            ["竜神ビッグバン"]       = new() { AssetName = "Enemy_RyujinBigBang",   Signature = new[] { S(FireTile, 2), S(TimeBomb, 1) },  Extra = new[] { S(GarbageDrop, 2) } },
            ["骸骨仮面グリム"]       = new() { AssetName = "Enemy_SkullGrim",       Signature = new[] { S(Clock, 3), S(BlackHoldBoost, 2), S(GarbageDrop, 2) }, FaceOverride = "Enemys01_6" },
            ["デストラクター山田"]   = new() { AssetName = "Enemy_DestructorYamada", Signature = new[] { S(GarbageDrop, 2), S(Rock, 1) }, Extra = new[] { S(Clock, 2) } },
            ["ブラッディゴリラ"]     = new() { AssetName = "Enemy_BloodyGorilla",   Signature = new[] { S(Shark, 1), S(Rock, 1) },         Extra = new[] { S(GarbageDrop, 2) }, FaceOverride = "Enemys01_2" },
            ["カオスエンペラー"]     = new() { AssetName = "Enemy_ChaosEmperor",    Signature = new[] { S(Confusion, 2), S(SkillSeal, 2), S(TimeBomb, 2) } },
            ["超神ゴジファイター"]   = new() { AssetName = "Enemy_GodziFighter",    Signature = new[] { S(FireTile, 2), S(Shark, 1) },     Extra = new[] { S(Clock, 2) } },
            ["魔王プリンスダーク"]   = new() { AssetName = "Enemy_PrinceDark",      Signature = new[] { S(BlackHoldBoost, 3), S(BlackHoldPower, 3), S(Confusion, 3) } },
            ["絶対王者ゴッドキング"] = new() { AssetName = "Enemy_GodKing",         Signature = new[] { S(Shark, 3), S(TimeBomb, 3), S(Clock, 3) } },
        };

        [MenuItem("WrestlePachi/敵データ移行（1回限り）")]
        private static void Migrate()
        {
            var list = AssetDatabase.LoadAssetAtPath<StageConfigList>(StageListPath);
            if (list == null)
            {
                EditorUtility.DisplayDialog("敵データ移行", $"{StageListPath} が見つかりません。", "OK");
                return;
            }

            var so     = new SerializedObject(list);
            var stages = so.FindProperty("_stages");

            // 表にない敵名があれば何も書き換えずに中断する
            var unknown = Enumerable.Range(0, stages.arraySize)
                .Select(i => stages.GetArrayElementAtIndex(i).FindPropertyRelative("LegacyEnemyName").stringValue)
                .Where(n => !Plans.ContainsKey(n))
                .Distinct()
                .ToList();
            if (unknown.Count > 0)
            {
                EditorUtility.DisplayDialog("敵データ移行", "割り当て表にない敵名があります：\n" + string.Join("\n", unknown), "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("敵データ移行",
                    $"{stages.arraySize} ステージの敵を EnemyData アセット（{EnemyFolder}）へ移行し、敵スキルを割り当てます。",
                    "実行する", "キャンセル"))
                return;

            if (!AssetDatabase.IsValidFolder(EnemyFolder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(EnemyFolder)!.Replace('\\', '/'), Path.GetFileName(EnemyFolder));

            var faces = AssetDatabase.LoadAllAssetsAtPath(FaceSheetPath).OfType<Sprite>().ToDictionary(s => s.name);

            var created = new Dictionary<string, EnemyData>();
            var log     = new StringBuilder("[EnemyDataMigration] 移行結果\n");
            var prevName = "";

            for (var i = 0; i < stages.arraySize; i++)
            {
                var stage      = stages.GetArrayElementAtIndex(i);
                var legacyName = stage.FindPropertyRelative("LegacyEnemyName").stringValue;
                var plan       = Plans[legacyName];

                if (!created.TryGetValue(legacyName, out var enemy))
                {
                    // 顔：上書き指定があればそれ、なければ最初に登場するステージの旧画像
                    var face = plan.FaceOverride != null
                        ? faces[plan.FaceOverride]
                        : stage.FindPropertyRelative("LegacyEnemySprite").objectReferenceValue as Sprite;
                    enemy = LoadOrCreate(plan.AssetName);
                    enemy.Configure(legacyName, face, plan.Signature, plan.Extra);
                    EditorUtility.SetDirty(enemy);
                    created[legacyName] = enemy;
                }

                var isRematch = legacyName == prevName;
                stage.FindPropertyRelative("Enemy").objectReferenceValue = enemy;
                stage.FindPropertyRelative("IsRematch").boolValue        = isRematch;

                // ステージ1のHPはテスト値（2500）が残っていたため戻す
                var hp = stage.FindPropertyRelative("EnemyHp");
                if (i == 0 && hp.intValue == 2500) hp.intValue = 20;

                var skills = EnemySkillRule.Resolve(enemy, isRematch);
                log.AppendLine($"{i + 1,2}: {plan.AssetName} {(isRematch ? "2戦目" : "1戦目")} HP{hp.intValue} 技=[{string.Join(", ", skills.Select(s => $"{s.Type}{s.Level}"))}]");
                prevName = legacyName;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>既存の敵アセットを読み込む。なければ新規作成する（冪等）</summary>
        private static EnemyData LoadOrCreate(string assetName)
        {
            var path  = $"{EnemyFolder}/{assetName}.asset";
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (enemy != null) return enemy;
            enemy = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(enemy, path);
            return enemy;
        }
    }
}
#nullable disable
#endif
