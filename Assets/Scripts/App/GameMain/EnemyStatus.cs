#nullable enable
using System;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 敵キャラクター1体分のステータス定義。
    /// EnemyStatusList にまとめて登録して使う。
    /// </summary>
    [Serializable]
    public sealed class EnemyStatus
    {
        [Header("基本情報")]
        [SerializeField] private string _enemyName = "敵";

        [Header("HP")]
        [SerializeField, Min(1)] private int _maxHp = 1000;

        /// <summary>敵の名前</summary>
        public string EnemyName => _enemyName;

        /// <summary>最大HP</summary>
        public int MaxHp => _maxHp;
    }
}
#nullable disable
