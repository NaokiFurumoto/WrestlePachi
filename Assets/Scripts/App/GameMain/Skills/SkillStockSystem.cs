using System;

namespace App.Skills
{
    /// <summary>
    /// 1ストック管理システム。
    /// 新しい保留が来たら上書き。天撃ボタン押下で手動消費する。
    /// </summary>
    public sealed class SkillStockSystem
    {
        private HoldType? _stock;

        public HoldType? Current  => _stock;
        public bool      HasStock => _stock.HasValue;

        /// <summary>ストック内容が変化したとき発火。null = 空になった。</summary>
        public event Action<HoldType?>? OnStockChanged;

        /// <summary>保留種別をストックする。既存ストックは上書き。</summary>
        public void SetStock(HoldType holdType)
        {
            _stock = holdType;
            OnStockChanged?.Invoke(_stock);
        }

        /// <summary>ストックを1つ取り出す。空なら false。</summary>
        public bool TryConsumeStock(out HoldType holdType)
        {
            if (!_stock.HasValue) { holdType = default; return false; }
            holdType = _stock.Value;
            _stock   = null;
            OnStockChanged?.Invoke(null);
            return true;
        }

        /// <summary>ゲームリセット時などに全クリア。</summary>
        public void ConsumeAll()
        {
            _stock = null;
            OnStockChanged?.Invoke(null);
        }
    }
}
