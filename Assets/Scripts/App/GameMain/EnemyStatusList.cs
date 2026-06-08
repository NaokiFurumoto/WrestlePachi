#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 全敵キャラクターのステータスをまとめて管理する ScriptableObject。
    /// Assets/Settings に1つ作成して EnemyController にアサインする。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyStatusList", menuName = "WrestlePachi/EnemyStatusList")]
    public sealed class EnemyStatusList : ScriptableObject
    {
        [SerializeField] private List<EnemyStatus> _enemies = new();

        public int Count => _enemies.Count;

        /// <summary>インデックスで敵ステータスを取得する。範囲外は null。</summary>
        public EnemyStatus? Get(int index)
            => index >= 0 && index < _enemies.Count ? _enemies[index] : null;
    }
}
#nullable disable
