#nullable enable
using System;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 1ステージ分のパラメーター。StageConfigList にまとめて登録する。
    /// </summary>
    [Serializable]
    public sealed class StageConfig
    {
        [Header("敵")]
        public string  EnemyName   = "";
        public Sprite? EnemySprite;
        [Min(1)] public int EnemyHp = 10;
        [Tooltip("0=Round表示なし（ラスボス等）、1以上=Round N と表示"), Min(0)]
        public int RoundNumber = 0;

        [Header("ボード")]
        [Tooltip("表示行数（10 or 12）"), Range(10, 12)]
        public int RowCount = 12;
        [Tooltip("ぷよ色数（4=赤黄緑青, 5=+紫）"), Range(4, 5)]
        public int ColorCount = 4;
        [Tooltip("ゲーム開始時に敷き詰めるお邪魔ぷよの行数（0=なし）"), Min(0)]
        public int GarbageInitialRows = 0;

        [Header("保留出現率（重み）  ※ 各色の重みの合計に対する比率で確率が決まる。例：赤2・他1×3 なら赤=2/5, 他=1/5。0=その色は出ない")]
        [Tooltip("赤保留の出現重み"), Min(0)] public int WeightRed    = 1;
        [Tooltip("黄保留の出現重み"), Min(0)] public int WeightYellow = 1;
        [Tooltip("緑保留の出現重み"), Min(0)] public int WeightGreen  = 1;
        [Tooltip("青保留の出現重み"), Min(0)] public int WeightBlue   = 1;
        [Tooltip("紫保留の出現重み（ColorCount=5 のステージのみ有効）"), Min(0)] public int WeightPurple = 0;

        [Header("虹保留")]
        [Tooltip("分母（20=1/20, 0=なし）"), Min(0)]
        public int RainbowDenominator = 50;

        [Header("黒保留")]
        [Tooltip("分母（20=1/20, 0=なし）"), Min(0)]
        public int BlackHoldDenominator = 0;

        [Header("黒ぷよ")]
        [Tooltip("連鎖消去1回ごとに落下する黒ぷよ数（0=なし）"), Min(0)]
        public int BlackPuyoCount = 0;

        [Header("ゲームパラメータ")]
        [Tooltip("ぷよ落下速度倍率（1=標準）"), Min(0.1f)]
        public float PuyoFallSpeed = 1f;
        [Tooltip("ぷよ1個消去あたりの発射球数"), Min(0f)]
        public float BallsPerPuyo  = 1f;
        [Tooltip("制限時間（秒, 0=無制限）"), Min(0f)]
        public float TimeLimit     = 180f;
    }

}
#nullable disable
