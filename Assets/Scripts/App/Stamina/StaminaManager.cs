#nullable enable
using System;
using GameSys;
using UnityEngine;

namespace App
{
    /// <summary>
    /// スタミナ管理 Singleton。
    /// ゲーム開始時に消費し、PlayerPrefs に保存する。
    /// 時間回復・広告・課金など回復手段は IStaminaRecovery で差し替え可能。
    /// </summary>
    public sealed class StaminaManager : BehaviourSingleton<StaminaManager>
    {
        private const string SaveKey     = "Stamina_Current";
        private const int    DefaultMax  = 5;

        [SerializeField, Min(1)] private int _maxStamina = DefaultMax;

        /// <summary>現在のスタミナ</summary>
        public int Current  { get; private set; }

        /// <summary>最大スタミナ</summary>
        public int Max      => _maxStamina;

        /// <summary>スタミナが変化したとき発火する（現在値, 最大値）</summary>
        public event Action<int, int>? OnChanged;

        // ─── BehaviourSingleton ───────────────────────────────────

        protected override void _SetInstance()
        {
            if (s_Instance == null)
                s_Instance = this;
            else if (s_Instance != this)
                Destroy(gameObject);
        }

        protected override void _OnAwake()
        {
            Current = PlayerPrefs.GetInt(SaveKey, _maxStamina);
            Current = Mathf.Clamp(Current, 0, _maxStamina);
        }

        // ─── 操作 ────────────────────────────────────────────────

        /// <summary>
        /// スタミナを消費する。
        /// 消費できた場合 true、スタミナ不足の場合 false を返す。
        /// </summary>
        public bool Consume(int amount = 1)
        {
            if (Current < amount) return false;
            Current -= amount;
            Save();
            OnChanged?.Invoke(Current, Max);
            return true;
        }

        /// <summary>スタミナを回復する（最大値を超えない）。</summary>
        public void Recover(int amount)
        {
            Current = Mathf.Min(Current + amount, _maxStamina);
            Save();
            OnChanged?.Invoke(Current, Max);
        }

        /// <summary>スタミナを最大値まで全回復する。</summary>
        public void RecoverFull()
        {
            Current = _maxStamina;
            Save();
            OnChanged?.Invoke(Current, Max);
        }

        // ─── 保存 ────────────────────────────────────────────────

        private void Save()
        {
            PlayerPrefs.SetInt(SaveKey, Current);
            PlayerPrefs.Save();
        }
    }
}
#nullable disable
