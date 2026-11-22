#nullable enable
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace App
{
    /// <summary>
    /// 全敵スキルのバランス数値（発動間隔・威力など）をまとめた ScriptableObject。
    /// GameModeConfig / EnemySkillAssets と同様、Assets に作成して GameMainController にアサインする。
    /// Playモード中でも Inspector から直接調整できるようにするための切り出し
    /// （元は各スキルクラスの private const だったが調整しにくいため移動した）。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySkillBalance", menuName = "WrestlePachi/EnemySkillBalance")]
    public sealed class EnemySkillBalanceConfig : ScriptableObject
    {
        [Serializable]
        public sealed class SharkBalance
        {
            [Tooltip("Lv1の発動間隔（秒）")]                 public float BaseIntervalSec = 12f;
            [Tooltip("Lvごとに間隔が短くなる秒数")]            public float IntervalStepSec = 1f;
            [Tooltip("発動間隔の下限（秒）")]                  public float MinIntervalSec  = 4f;
            [Tooltip("Lv1の消去数")]                          public int   BaseCount       = 2;
            [Tooltip("ぷよ1個消去あたりの敵HP回復量")]         public int   HealPerPuyo     = 3;
        }

        [Serializable]
        public sealed class FireTileBalance
        {
            [Tooltip("Lv1の炎マス数（Lvごとに+1）")] public int BaseTileCount = 1;
        }

        [Serializable]
        public sealed class GarbageDropBalance
        {
            [Tooltip("Lv1の発動間隔（秒）")]      public float BaseIntervalSec = 20f;
            [Tooltip("Lvごとに間隔が短くなる秒数")] public float IntervalStepSec = 2f;
            [Tooltip("発動間隔の下限（秒）")]       public float MinIntervalSec  = 5f;
            [Tooltip("Lv1の落下数（Lvごとに+1）")]  public int   BaseCount       = 1;
        }

        [Serializable]
        public sealed class RockBalance
        {
            [Tooltip("Lv1の発動間隔（秒）")]      public float BaseIntervalSec = 15f;
            [Tooltip("Lvごとに間隔が短くなる秒数")] public float IntervalStepSec = 1.5f;
            [Tooltip("発動間隔の下限（秒）")]       public float MinIntervalSec  = 5f;
            [Tooltip("Lv1の耐久値（Lvごとに+1）")]  public int   BaseDurability  = 2;
        }

        [Serializable]
        public sealed class TimeBombBalance
        {
            [Tooltip("Lv1の設置間隔（秒）")]       public float BaseIntervalSec = 18f;
            [Tooltip("Lvごとに間隔が短くなる秒数")] public float IntervalStepSec = 2f;
            [Tooltip("設置間隔の下限（秒）")]       public float MinIntervalSec  = 6f;
            [Tooltip("設置から爆発までの秒数（Lv影響なし）")] public float FuseSeconds = 8f;
        }

        [Serializable]
        public sealed class SkillSealBalance
        {
            [Tooltip("封印対象の優先順（レベルが低いうちは先頭からLv個ぶんを封印）")]
            public HoldType[] SealOrder =
            {
                HoldType.Red, HoldType.Yellow, HoldType.Green,
                HoldType.Blue, HoldType.Purple, HoldType.Black,
            };
        }

        [Serializable]
        public sealed class ConfusionBalance
        {
            [Tooltip("発動間隔（秒、Lv影響なし・要調整）")] public float BaseIntervalSec = 15f;
            [Tooltip("Lv1の持続時間（秒）")]                public float BaseDurationSec = 5f;
            [Tooltip("Lvごとに持続時間が伸びる秒数")]        public float DurationStepSec = 1f;
        }

        [Serializable]
        public sealed class BlackHoldBoostBalance
        {
            [Tooltip("Lv1の黒保留出現分母")]        public int BaseDenominator = 20;
            [Tooltip("Lvごとに分母が下がる量")]      public int DenominatorStep = 2;
            [Tooltip("分母の下限（これ以上出やすくならない）")] public int MinDenominator = 4;
        }

        [Serializable]
        public sealed class BlackHoldPowerBalance
        {
            [Tooltip("Lv1の黒保留お邪魔落下数")]   public int BaseOjamaCount = 6;
            [Tooltip("Lvごとに落下数が増える量")]  public int OjamaCountStep = 2;
        }

        [Serializable]
        public sealed class ClockBalance
        {
            [Tooltip("Lv1の発動間隔（秒）")]        public float BaseIntervalSec       = 15f;
            [Tooltip("Lvごとに間隔が短くなる秒数")]  public float IntervalStepSec       = 1f;
            [Tooltip("発動間隔の下限（秒）")]        public float MinIntervalSec        = 5f;
            [Tooltip("Lv1で削る秒数")]              public float BaseReduceSeconds     = 5f;
            [Tooltip("Lvごとに削る秒数が増える量")]  public float ReduceSecondsPerLevel = 1f;
        }

        [Header("サメ")]       public SharkBalance          Shark          = new();
        [Header("炎")]         public FireTileBalance       FireTile       = new();
        [Header("お邪魔落下")] public GarbageDropBalance    GarbageDrop    = new();
        [Header("岩")]
        [FormerlySerializedAs("Pineapple")]
        public RockBalance Rock = new();
        [Header("時限爆弾")]   public TimeBombBalance       TimeBomb       = new();
        [Header("スキル封印")] public SkillSealBalance      SkillSeal      = new();
        [Header("混乱")]       public ConfusionBalance      Confusion      = new();
        [Header("黒保留強化")] public BlackHoldBoostBalance BlackHoldBoost = new();
        [Header("黒保留威力")] public BlackHoldPowerBalance BlackHoldPower = new();
        [Header("時計")]       public ClockBalance          Clock          = new();
    }
}
#nullable disable
