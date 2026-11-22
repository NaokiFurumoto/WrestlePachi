#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// WrestlePachi メニュー → ステージデータ自動生成 で
    /// Assets/Settings/StageConfigList.asset にこのファイル内の BuildStages() の内容を書き込む。
    ///
    /// ⚠️ 2026-08-15時点、Assets/Resources/StageConfigList.asset には本番の30ステージ分データ
    /// （実際の敵名・10ステージ分のスプライト）が既に手動で入っている。このメニューを実行すると
    /// それが BuildStages() の仮データ（25ステージ）で完全に上書きされる。
    /// 今後のステージデータ編集は Inspector で直接行う方針とし、このツールは開発初期の名残として
    /// 残しているだけなので、実行時は必ず確認ダイアログで止める。
    /// </summary>
    public static class StageConfigPopulatorEditor
    {
        private const string AssetPath = "Assets/Resources/StageConfigList.asset";

        // アセットが存在しない場合のみ自動生成（起動・再コンパイル時）。
        // 既にアセットがある今の状態では実行されない（後方互換のため残置）。
        [InitializeOnLoadMethod]
        private static void AutoPopulate()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<StageConfigList>(AssetPath) == null)
                    Populate();
            };
        }

        [MenuItem("WrestlePachi/ステージデータ自動生成（要確認：既存データを上書き）")]
        private static void Populate()
        {
            var list = AssetDatabase.LoadAssetAtPath<StageConfigList>(AssetPath);

            if (list != null)
            {
                var confirmed = EditorUtility.DisplayDialog(
                    "ステージデータの上書き確認",
                    $"{AssetPath} には既に本番データが入っている可能性があります。\n\n" +
                    "このメニューを実行すると、現在の内容（敵名・スプライト・敵スキル設定等）が\n" +
                    "このファイル内 BuildStages() の仮データで完全に上書きされます。\n\n" +
                    "本当に実行しますか？",
                    "上書きする（元に戻せません）", "キャンセル");
                if (!confirmed) return;
            }

            if (list == null)
            {
                list = ScriptableObject.CreateInstance<StageConfigList>();
                AssetDatabase.CreateAsset(list, AssetPath);
                AssetDatabase.SaveAssets();
            }

            var so     = new SerializedObject(list);
            var stages = so.FindProperty("_stages");
            stages.ClearArray();

            var data = BuildStages();
            for (int i = 0; i < data.Count; i++)
            {
                stages.InsertArrayElementAtIndex(i);
                WriteStage(stages.GetArrayElementAtIndex(i), data[i]);
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssets();

            Debug.Log($"[StageConfigPopulator] {data.Count} ステージを {AssetPath} に書き込みました。スプライトは手動でアサインしてください。");
            Selection.activeObject = list;
        }

        private static void WriteStage(SerializedProperty p, StageConfig s)
        {
            p.FindPropertyRelative("EnemyName"          ).stringValue = s.EnemyName;
            p.FindPropertyRelative("EnemyHp"            ).intValue    = s.EnemyHp;
            p.FindPropertyRelative("EnemyLevel"          ).intValue    = s.EnemyLevel;
            p.FindPropertyRelative("RowCount"           ).intValue    = s.RowCount;
            p.FindPropertyRelative("ColorCount"         ).intValue    = s.ColorCount;
            p.FindPropertyRelative("GarbageInitialRows" ).intValue    = s.GarbageInitialRows;
            p.FindPropertyRelative("WeightRed"          ).intValue    = s.WeightRed;
            p.FindPropertyRelative("WeightYellow"       ).intValue    = s.WeightYellow;
            p.FindPropertyRelative("WeightGreen"        ).intValue    = s.WeightGreen;
            p.FindPropertyRelative("WeightBlue"         ).intValue    = s.WeightBlue;
            p.FindPropertyRelative("WeightPurple"       ).intValue    = s.WeightPurple;
            p.FindPropertyRelative("RainbowDenominator" ).intValue    = s.RainbowDenominator;
            p.FindPropertyRelative("PuyoFallSpeed"      ).floatValue  = s.PuyoFallSpeed;
            p.FindPropertyRelative("BallsPerPuyo"       ).floatValue  = s.BallsPerPuyo;
            p.FindPropertyRelative("TimeLimit"          ).floatValue  = s.TimeLimit;
            WriteSkills(p.FindPropertyRelative("EnemySkills"), s.EnemySkills);
        }

        // 敵スキルは Type+Level のペア配列。Stage(...) の skills 引数で渡した内容をそのまま書き込む
        private static void WriteSkills(SerializedProperty arrayProp, EnemySkillEntry[] skills)
        {
            arrayProp.ClearArray();
            for (var i = 0; i < skills.Length; i++)
            {
                arrayProp.InsertArrayElementAtIndex(i);
                var elem = arrayProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("Type").enumValueIndex = (int)skills[i].Type;
                elem.FindPropertyRelative("Level").intValue      = skills[i].Level;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // 25 ステージ定義（四天王A〜Eは一旦保留中、下記コメントアウト箇所を参照）
        //   難易度の主なレバーと導入タイミング：
        //     HP・時間・玉倍率・虹分母 → 全体を通して緩やかに変化
        //     5色            → Stage 14 から導入（最大の体感ジャンプ）
        //     お邪魔初期配置  → Stage 18 から 1行
        //     10行           → Stage 21 から縮小
        //     落下速度        → Stage 20 以降で徐々に加速
        //   ※ 盤面へのデバフ（お邪魔以外）が欲しい場合は StageConfig にフィールドを足すのではなく
        //     敵スキル（EnemySkillType、skills引数）として実装する方針。
        //   ※ 黒保留の出現率は敵スキル BlackHoldBoost に移管済み（2026-08-15）。
        //     どのステージに付けるかは未定。必要になったらStage呼び出しに
        //     skills: new[] { new EnemySkillEntry { Type = EnemySkillType.BlackHoldBoost, Level = N } } を追加する。
        // ─────────────────────────────────────────────────────────────────
        private static List<StageConfig> BuildStages() => new List<StageConfig>
        {
            //           名前           HP   行 色 邪魔  虹分母 速度   玉倍   時間
            // ── 1〜5  チュートリアル帯 ──────────────────────────────────────────
            Stage("ザコA",             10,  12, 4,  0,  20, 1.00f, 1.30f, 240),
            Stage("ザコB",             12,  12, 4,  0,  22, 1.00f, 1.25f, 235),
            Stage("ザコC",             15,  12, 4,  0,  25, 1.00f, 1.20f, 230),
            Stage("ザコD",             18,  12, 4,  0,  27, 1.00f, 1.15f, 225),
            Stage("ザコE",             22,  12, 4,  0,  30, 1.00f, 1.10f, 220),
            // ── 6〜10  初級 ────────────────────────────────────────────────────
            Stage("中ボスA",           28,  12, 4,  0,  33, 1.00f, 1.05f, 215),
            Stage("中ボスB",           35,  12, 4,  0,  36, 1.00f, 1.00f, 210),
            Stage("中ボスC",           43,  12, 4,  0,  40, 1.00f, 0.97f, 205),
            Stage("中ボスD",           52,  12, 4,  0,  43, 1.00f, 0.95f, 200),
            Stage("中ボスE",           62,  12, 4,  0,  45, 1.00f, 0.92f, 195),
            // ── 11〜13  中級前半（4色） ────────────────────────────────────────
            Stage("強敵A",             74,  12, 4,  0,  48, 1.00f, 0.90f, 192),
            Stage("強敵B",             87,  12, 4,  0,  50, 1.00f, 0.88f, 189),
            Stage("強敵C",            100,  12, 4,  0,  52, 1.00f, 0.87f, 186),
            // ── 14〜17  中級後半（5色導入） ────────────────────────────────────
            Stage("強敵D",            115,  12, 5,  0,  55, 1.00f, 0.85f, 183),
            Stage("強敵E",            130,  12, 5,  0,  58, 1.00f, 0.83f, 180),
            Stage("幹部A",            148,  12, 5,  0,  61, 1.00f, 0.81f, 177),
            Stage("幹部B",            167,  12, 5,  0,  64, 1.00f, 0.79f, 174),
            // ── 18〜19  上級前半（お邪魔初期配置導入） ─────────────────────────
            Stage("幹部C",            187,  12, 5,  1,  67, 1.00f, 0.77f, 171),
            Stage("幹部D",            208,  12, 5,  1,  70, 1.00f, 0.75f, 168),
            // ── 20  上級後半（落下加速開始） ───────────────────────────────────
            Stage("幹部E",            230,  12, 5,  1,  73, 1.10f, 0.73f, 165),
            // 四天王A〜E：一旦保留（不要になったので抜いた。復活させる場合はコメント解除して
            // 前後のHP・虹分母・速度カーブと繋がるよう調整すること）
            // Stage("四天王A",          255,  12, 5,  1,  76, 1.10f, 0.71f, 162),
            // Stage("四天王B",          282,  12, 5,  1,  80, 1.10f, 0.69f, 159),
            // Stage("四天王C",          311,  12, 5,  1,  84, 1.20f, 0.67f, 156),
            // Stage("四天王D",          342,  10, 5,  1,  88, 1.20f, 0.65f, 153),
            // Stage("四天王E",          375,  10, 5,  1,  90, 1.20f, 0.62f, 150),
            // ── 21〜23  超上級（10行縮小） ─────────────────────────────────────
            Stage("最強幹部A",        405,  10, 5,  2,  93, 1.30f, 0.59f, 147),
            Stage("最強幹部B",        430,  10, 5,  2,  95, 1.40f, 0.56f, 144),
            Stage("最強幹部C",        455,  10, 5,  2,  97, 1.40f, 0.53f, 141),
            // ── 24〜25  地獄（落下速度最大） ───────────────────────────────────
            Stage("ラスボス前座",     478,  10, 5,  2,  98, 1.50f, 0.52f, 138),
            Stage("ラスボス",         500,  10, 5,  2,  99, 1.50f, 0.50f, 135),
        };

        // level 引数：同じ敵（名前・画像）を複数ステージで使い回す際に「Lv.N」として差別化する（デフォルト1）
        // skills 引数の例：
        //   Stage("四天王A", 255, 12, 5, 1, 76, 1.10f, 0.69f, 159,
        //       skills: new[] { new EnemySkillEntry { Type = EnemySkillType.Shark, Level = 2 } })
        private static StageConfig Stage(
            string name, int hp, int rows, int colors, int garbage,
            int rainbow, float speed, float bpp, float time,
            EnemySkillEntry[] skills = null, int level = 1)
        {
            return new StageConfig
            {
                EnemyName          = name,
                EnemyHp            = hp,
                EnemyLevel         = level,
                RowCount           = rows,
                ColorCount         = colors,
                GarbageInitialRows = garbage,
                WeightRed          = 1,
                WeightYellow       = 1,
                WeightGreen        = 1,
                WeightBlue         = 1,
                WeightPurple       = 1,
                RainbowDenominator = rainbow,
                PuyoFallSpeed      = speed,
                BallsPerPuyo       = bpp,
                TimeLimit          = time,
                EnemySkills        = skills ?? Array.Empty<EnemySkillEntry>(),
            };
        }
    }
}
#endif
