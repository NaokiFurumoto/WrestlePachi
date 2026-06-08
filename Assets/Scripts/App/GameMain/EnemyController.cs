#nullable enable
using System;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 敵のHP管理を担うコンポーネント。
    /// TakeDamage でダメージを受け、HP が 0 になると OnDefeated を発火する。
    /// SetEnemy で次の敵に切り替えられる。
    /// </summary>
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyStatusList? _statusList;
        [SerializeField] private int              _enemyIndex;

        /// <summary>HP が変化したとき発火する（現在HP, 最大HP）</summary>
        public event Action<int, int>? OnHpChanged;

        /// <summary>HP が 0 になったとき発火する</summary>
        public event Action? OnDefeated;

        /// <summary>現在HP</summary>
        public int CurrentHp { get; private set; }

        /// <summary>最大HP</summary>
        public int MaxHp => CurrentStatus?.MaxHp ?? 0;

        /// <summary>現在の敵名</summary>
        public string EnemyName => CurrentStatus?.EnemyName ?? string.Empty;

        /// <summary>撃破済みかどうか</summary>
        public bool IsDefeated => CurrentHp <= 0;

        private EnemyStatus? CurrentStatus => _statusList?.Get(_enemyIndex);

        private void Awake() => ResetHp();

        /// <summary>
        /// 指定インデックスの敵に切り替え、HPをリセットする。
        /// </summary>
        public void SetEnemy(int index)
        {
            _enemyIndex = index;
            ResetHp();
        }

        /// <summary>
        /// ダメージを受ける。既に撃破済みの場合は何もしない。
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (IsDefeated) return;
            CurrentHp = Mathf.Max(0, CurrentHp - damage);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            if (CurrentHp <= 0) OnDefeated?.Invoke();
        }

        /// <summary>
        /// 虹スキル専用：HP を即座に 0 にして撃破する。
        /// </summary>
        public void InstantKill()
        {
            if (IsDefeated) return;
            CurrentHp = 0;
            OnHpChanged?.Invoke(0, MaxHp);
            OnDefeated?.Invoke();
        }

        private void ResetHp()
        {
            var status = CurrentStatus;
            CurrentHp = status != null ? status.MaxHp : 0;
        }
    }
}
#nullable disable
