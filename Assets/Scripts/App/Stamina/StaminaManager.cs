#nullable enable
using System;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;

namespace App
{
    /// <summary>
    /// スタミナ管理 Singleton。
    /// ゲーム開始時に消費し、PlayerPrefs に保存する。
    /// 時間経過で自動回復し、毎秒 OnRecoveryTick を発火する。
    /// </summary>
    public sealed class StaminaManager : BehaviourSingleton<StaminaManager>
    {
        private const string SaveKey             = "Stamina_Current";
        private const string NextRecoverySaveKey = "Stamina_NextRecoveryTime";
        private const int    DefaultMax          = 5;

        [SerializeField, Min(1)]   private int   _maxStamina              = DefaultMax;
        [SerializeField, Min(0.1f)] private float _recoveryIntervalMinutes = 5f;

        /// <summary>現在のスタミナ</summary>
        public int Current { get; private set; }

        /// <summary>最大スタミナ</summary>
        public int Max => _maxStamina;

        /// <summary>スタミナが変化したとき発火する（現在値, 最大値）</summary>
        public event Action<int, int>? OnChanged;

        /// <summary>毎秒発火。引数は次回回復までの残り秒数。満タン時は発火しない。</summary>
        public event Action<int>? OnRecoveryTick;

        private long _nextRecoveryTime; // UnixTime（秒）

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

            long.TryParse(PlayerPrefs.GetString(NextRecoverySaveKey, "0"), out _nextRecoveryTime);

            ProcessPendingRecovery();
            StartRecoveryLoopAsync(destroyCancellationToken).Forget();
        }

        // ─── 操作 ────────────────────────────────────────────────

        /// <summary>
        /// スタミナを消費する。
        /// 消費できた場合 true、スタミナ不足の場合 false を返す。
        /// </summary>
        public bool Consume(int amount = 1)
        {
            if (Current < amount) return false;
            var wasMax = Current >= Max;
            Current -= amount;
            if (wasMax)
            {
                // 満タンから減った → 回復タイマー開始
                _nextRecoveryTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + IntervalSeconds;
                SaveRecoveryTime();
            }
            Save();
            OnChanged?.Invoke(Current, Max);
            return true;
        }

        /// <summary>スタミナを回復する（最大値を超えない）。</summary>
        public void Recover(int amount)
        {
            Current = Mathf.Min(Current + amount, _maxStamina);
            if (Current >= Max) _nextRecoveryTime = 0;
            Save();
            OnChanged?.Invoke(Current, Max);
        }

        /// <summary>スタミナを最大値まで全回復する。</summary>
        public void RecoverFull()
        {
            Current = _maxStamina;
            _nextRecoveryTime = 0;
            Save();
            SaveRecoveryTime();
            OnChanged?.Invoke(Current, Max);
        }

        /// <summary>次回回復までの残り秒数。満タン時は 0 を返す。</summary>
        public int GetRemainingSeconds()
        {
            if (Current >= Max || _nextRecoveryTime == 0) return 0;
            var remaining = _nextRecoveryTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return (int)Math.Max(0L, remaining);
        }

        // ─── 回復ループ ──────────────────────────────────────────

        private long IntervalSeconds => (long)(_recoveryIntervalMinutes * 60f);

        private async UniTaskVoid StartRecoveryLoopAsync(System.Threading.CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(1000, DelayType.Realtime, cancellationToken: ct)
                    .SuppressCancellationThrow();
                if (ct.IsCancellationRequested) break;

                ProcessPendingRecovery();

                if (Current < Max)
                    OnRecoveryTick?.Invoke(GetRemainingSeconds());
            }
        }

        private void ProcessPendingRecovery()
        {
            if (Current >= Max) return;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (_nextRecoveryTime == 0)
            {
                // タイマー未設定 → 今から1インターバル後を設定
                _nextRecoveryTime = now + IntervalSeconds;
                SaveRecoveryTime();
                return;
            }

            bool recovered = false;
            while (_nextRecoveryTime <= now && Current < Max)
            {
                Current++;
                _nextRecoveryTime += IntervalSeconds;
                recovered = true;
            }

            if (Current >= Max) _nextRecoveryTime = 0;

            if (!recovered) return;
            Save();
            SaveRecoveryTime();
            OnChanged?.Invoke(Current, Max);
        }

        // ─── 保存 ────────────────────────────────────────────────

        private void Save()
        {
            PlayerPrefs.SetInt(SaveKey, Current);
            PlayerPrefs.Save();
        }

        private void SaveRecoveryTime()
        {
            PlayerPrefs.SetString(NextRecoverySaveKey, _nextRecoveryTime.ToString());
            PlayerPrefs.Save();
        }
    }
}
#nullable disable
