#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// WrestlePachi メニュー → ステージデータ自動生成 で
    /// Assets/Settings/StageConfigList.asset に 30 ステージ分のデータを書き込む。
    /// スプライトだけ手動でアサインすること。
    /// </summary>
    public static class StageConfigPopulatorEditor
    {
        private const string AssetPath = "Assets/Resources/StageConfigList.asset";

        // アセットが存在しない場合のみ自動生成（起動・再コンパイル時）
        [InitializeOnLoadMethod]
        private static void AutoPopulate()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<StageConfigList>(AssetPath) == null)
                    Populate();
            };
        }

        [MenuItem("WrestlePachi/ステージデータ自動生成")]
        private static void Populate()
        {
            var list = AssetDatabase.LoadAssetAtPath<StageConfigList>(AssetPath);
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
            p.FindPropertyRelative("RowCount"           ).intValue    = s.RowCount;
            p.FindPropertyRelative("ColorCount"         ).intValue    = s.ColorCount;
            p.FindPropertyRelative("GarbageInitialRows" ).intValue    = s.GarbageInitialRows;
            p.FindPropertyRelative("WeightRed"          ).intValue    = s.WeightRed;
            p.FindPropertyRelative("WeightYellow"       ).intValue    = s.WeightYellow;
            p.FindPropertyRelative("WeightGreen"        ).intValue    = s.WeightGreen;
            p.FindPropertyRelative("WeightBlue"         ).intValue    = s.WeightBlue;
            p.FindPropertyRelative("WeightPurple"       ).intValue    = s.WeightPurple;
            p.FindPropertyRelative("RainbowDenominator"   ).intValue    = s.RainbowDenominator;
            p.FindPropertyRelative("BlackHoldDenominator").intValue    = s.BlackHoldDenominator;
            p.FindPropertyRelative("BlackPuyoCount"      ).intValue    = s.BlackPuyoCount;
            p.FindPropertyRelative("PuyoFallSpeed"      ).floatValue  = s.PuyoFallSpeed;
            p.FindPropertyRelative("BallsPerPuyo"       ).floatValue  = s.BallsPerPuyo;
            p.FindPropertyRelative("TimeLimit"          ).floatValue  = s.TimeLimit;
        }

        // ─────────────────────────────────────────────────────────────────
        // 30 ステージ定義
        //   難易度の主なレバーと導入タイミング：
        //     HP・時間・玉倍率・虹分母 → 全体を通して緩やかに変化
        //     5色            → Stage 14 から導入（最大の体感ジャンプ）
        //     お邪魔初期配置  → Stage 18 から 1行
        //     黒ぷよ         → Stage 20 から導入
        //     10行           → Stage 24 から縮小
        //     落下速度        → Stage 20 以降で徐々に加速
        //   ※ 行数・お邪魔・黒ぷよ・落下速度は現在未実装。
        //     データとして持っておき、実装後に自動で有効になる。
        // ─────────────────────────────────────────────────────────────────
        private static List<StageConfig> BuildStages() => new List<StageConfig>
        {
            //           名前           HP   行 色 邪魔  虹分母 黒保留 黒ぷよ 速度   玉倍   時間
            // ── 1〜5  チュートリアル帯 ──────────────────────────────────────────
            Stage("ザコA",             10,  12, 4,  0,  20,    0,    0, 1.00f, 1.30f, 240),
            Stage("ザコB",             12,  12, 4,  0,  22,    0,    0, 1.00f, 1.25f, 235),
            Stage("ザコC",             15,  12, 4,  0,  25,    0,    0, 1.00f, 1.20f, 230),
            Stage("ザコD",             18,  12, 4,  0,  27,    0,    0, 1.00f, 1.15f, 225),
            Stage("ザコE",             22,  12, 4,  0,  30,    0,    0, 1.00f, 1.10f, 220),
            // ── 6〜10  初級 ────────────────────────────────────────────────────
            Stage("中ボスA",           28,  12, 4,  0,  33,    0,    0, 1.00f, 1.05f, 215),
            Stage("中ボスB",           35,  12, 4,  0,  36,    0,    0, 1.00f, 1.00f, 210),
            Stage("中ボスC",           43,  12, 4,  0,  40,    0,    0, 1.00f, 0.97f, 205),
            Stage("中ボスD",           52,  12, 4,  0,  43,    0,    0, 1.00f, 0.95f, 200),
            Stage("中ボスE",           62,  12, 4,  0,  45,    0,    0, 1.00f, 0.92f, 195),
            // ── 11〜13  中級前半（4色） ────────────────────────────────────────
            Stage("強敵A",             74,  12, 4,  0,  48,    0,    0, 1.00f, 0.90f, 192),
            Stage("強敵B",             87,  12, 4,  0,  50,    0,    0, 1.00f, 0.88f, 189),
            Stage("強敵C",            100,  12, 4,  0,  52,    0,    0, 1.00f, 0.87f, 186),
            // ── 14〜17  中級後半（5色導入） ────────────────────────────────────
            Stage("強敵D",            115,  12, 5,  0,  55,    0,    0, 1.00f, 0.85f, 183),
            Stage("強敵E",            130,  12, 5,  0,  58,    0,    0, 1.00f, 0.83f, 180),
            Stage("幹部A",            148,  12, 5,  0,  61,    0,    0, 1.00f, 0.81f, 177),
            Stage("幹部B",            167,  12, 5,  0,  64,    0,    0, 1.00f, 0.79f, 174),
            // ── 18〜19  上級前半（お邪魔初期配置導入） ─────────────────────────
            Stage("幹部C",            187,  12, 5,  1,  67,    0,    0, 1.00f, 0.77f, 171),
            Stage("幹部D",            208,  12, 5,  1,  70,    0,    0, 1.00f, 0.75f, 168),
            // ── 20〜23  上級後半（黒保留・黒ぷよ導入・落下加速） ───────────────
            Stage("幹部E",            230,  12, 5,  1,  73,   50,    1, 1.10f, 0.73f, 165),
            Stage("四天王A",          255,  12, 5,  1,  76,   47,    1, 1.10f, 0.71f, 162),
            Stage("四天王B",          282,  12, 5,  1,  80,   44,    1, 1.10f, 0.69f, 159),
            Stage("四天王C",          311,  12, 5,  1,  84,   41,    2, 1.20f, 0.67f, 156),
            // ── 24〜26  超上級（10行縮小） ─────────────────────────────────────
            Stage("四天王D",          342,  10, 5,  1,  88,   38,    2, 1.20f, 0.65f, 153),
            Stage("四天王E",          375,  10, 5,  1,  90,   35,    2, 1.20f, 0.62f, 150),
            Stage("最強幹部A",        405,  10, 5,  2,  93,   32,    2, 1.30f, 0.59f, 147),
            // ── 27〜30  地獄（落下速度最大） ───────────────────────────────────
            Stage("最強幹部B",        430,  10, 5,  2,  95,   28,    3, 1.40f, 0.56f, 144),
            Stage("最強幹部C",        455,  10, 5,  2,  97,   24,    3, 1.40f, 0.53f, 141),
            Stage("ラスボス前座",     478,  10, 5,  2,  98,   20,    3, 1.50f, 0.52f, 138),
            Stage("ラスボス",         500,  10, 5,  2,  99,   18,    3, 1.50f, 0.50f, 135),
        };

        private static StageConfig Stage(
            string name, int hp, int rows, int colors, int garbage,
            int rainbow, int blackHold, int blackPuyo, float speed, float bpp, float time)
        {
            return new StageConfig
            {
                EnemyName            = name,
                EnemyHp              = hp,
                RowCount             = rows,
                ColorCount           = colors,
                GarbageInitialRows   = garbage,
                WeightRed            = 1,
                WeightYellow         = 1,
                WeightGreen          = 1,
                WeightBlue           = 1,
                WeightPurple         = 1,
                RainbowDenominator   = rainbow,
                BlackHoldDenominator = blackHold,
                BlackPuyoCount       = blackPuyo,
                PuyoFallSpeed        = speed,
                BallsPerPuyo         = bpp,
                TimeLimit            = time,
            };
        }
    }
}
#endif
