using System;
using System.Collections.Generic;

namespace App.Skills
{
    /// <summary>
    /// スキルストック管理システム。
    /// CanExecute=false のスキルを最大5つストックし、天撃ボタンで任意発動できる。
    /// MAX到達時は OnMaxReached イベントで全消しを通知する。
    /// </summary>
    public sealed class SkillStockSystem
    {
        private const int MaxStocks = 5;
        private readonly Queue<HoldType> _stocks = new();

        public int Count => _stocks.Count;
        public bool IsFull => _stocks.Count >= MaxStocks;
        public bool IsEmpty => _stocks.Count == 0;

        /// <summary>ストック数が変化したときに発火する（現在のストック数を渡す）。</summary>
        public event Action<int>? OnStockChanged;

        /// <summary>MAX（5個）到達したときに発火する（全消し自動発動に使う）。</summary>
        public event Action? OnMaxReached;

        /// <summary>ストックに積む。満杯なら false を返す。</summary>
        public bool TryAddStock(HoldType holdType)
        {
            if (IsFull) return false;
            _stocks.Enqueue(holdType);
            OnStockChanged?.Invoke(Count);
            if (IsFull) OnMaxReached?.Invoke();
            return true;
        }

        /// <summary>ストックから1つ取り出す（FIFO）。空なら false を返す。</summary>
        public bool TryConsumeStock(out HoldType holdType)
        {
            if (IsEmpty) { holdType = default; return false; }
            holdType = _stocks.Dequeue();
            OnStockChanged?.Invoke(Count);
            return true;
        }

        /// <summary>全消し発動時に呼ぶ。全ストックを消費する。</summary>
        public void ConsumeAll()
        {
            _stocks.Clear();
            OnStockChanged?.Invoke(0);
        }
    }
}
