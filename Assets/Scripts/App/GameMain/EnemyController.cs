#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private int _enemyIndex;

        /// <summary>HP が変化したとき発火する（現在HP, 最大HP）</summary>
        public event Action<int, int>? OnHpChanged;

        /// <summary>ダメージを受けたとき発火する（ダメージ量）</summary>
        public event Action<int>? OnDamaged;

        /// <summary>回復したとき発火する（実際に回復した量）</summary>
        public event Action<int>? OnHealed;

        /// <summary>HP が 0 になったとき発火する</summary>
        public event Action? OnDefeated;

        /// <summary>敵が切り替わったとき発火する（顔スプライト）</summary>
        public event Action<Sprite?>? OnEnemySet;

        /// <summary>現在HP</summary>
        public int CurrentHp { get; private set; }

        /// <summary>最大HP（デバッグで体力を上書きしている場合はその値）</summary>
        public int MaxHp => _debugMaxHp ?? CurrentStage?.EnemyHp ?? 0;

        /// <summary>デバッグ用の体力上書き（エディタのデバッグウィンドウからのみ設定する）</summary>
        private int? _debugMaxHp;
        private int? _debugStartHp;

#if UNITY_EDITOR
        /// <summary>
        /// デバッグ用：敵の体力を上書きする。ステージが変わっても、最大 maxHp・開始時 currentHp のまま続く。
        /// maxHp を null にすると元の体力（満タン）に戻す。
        /// </summary>
        public void Debug_SetHp(int? maxHp, int currentHp)
        {
            _debugMaxHp   = maxHp;
            _debugStartHp = maxHp.HasValue ? currentHp : null;
            CurrentHp     = Mathf.Clamp(currentHp, 1, MaxHp);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }
#endif

        /// <summary>現在の敵名</summary>
        public string EnemyName => CurrentStage?.EnemyName ?? string.Empty;

        /// <summary>現在の顔スプライト</summary>
        public Sprite? FaceSprite => CurrentStage?.EnemySprite;

        /// <summary>撃破済みかどうか</summary>
        public bool IsDefeated => CurrentHp <= 0;

        /// <summary>現在の敵インデックス（0始まり）</summary>
        public int EnemyIndex => _enemyIndex;

        /// <summary>敵の総数（= ステージ数）</summary>
        public int TotalCount => StageConfigList.Instance?.Count ?? 0;

        private StageConfig? CurrentStage => StageConfigList.Instance?.Get(_enemyIndex);

        private void Awake()
        {
            ResetHp();
            OnEnemySet?.Invoke(FaceSprite);
        }

        /// <summary>
        /// 指定インデックスの敵に切り替え、HPをリセットする。
        /// </summary>
        public void SetEnemy(int index)
        {
            _enemyIndex = index;
            ResetHp();
            OnEnemySet?.Invoke(FaceSprite);
        }

        /// <summary>
        /// ダメージを受ける。既に撃破済みの場合は何もしない。
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (IsDefeated) return;
            CurrentHp = Mathf.Max(0, CurrentHp - damage);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnDamaged?.Invoke(damage);
            if (CurrentHp <= 0) OnDefeated?.Invoke();
        }

        /// <summary>
        /// HP を回復する。敵スキル（サメ等）から呼ぶ。最大HPを超えて回復はしない。
        /// </summary>
        public void Heal(int amount)
        {
            if (IsDefeated || amount <= 0) return;
            var before = CurrentHp;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);

            // 実際に回復した量だけ通知する（HP満タンで0なら通知しない）
            var healed = CurrentHp - before;
            if (healed > 0) OnHealed?.Invoke(healed);
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

        /// <summary>
        /// やられ演出を再生して完了を待つ。
        /// Animator や SE は後からここに追加する。
        /// </summary>
        public async UniTask PlayDefeatAsync(CancellationToken ct)
        {
            await UniTask.Delay(1000, DelayType.Realtime, cancellationToken: ct);
        }

        private void ResetHp()
        {
            CurrentHp = Mathf.Min(_debugStartHp ?? MaxHp, MaxHp);
        }
    }
}
#nullable disable
