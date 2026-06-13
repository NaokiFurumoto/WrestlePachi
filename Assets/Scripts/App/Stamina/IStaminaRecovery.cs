#nullable enable
using Cysharp.Threading.Tasks;

namespace App
{
    /// <summary>
    /// スタミナ回復手段の差し込み口。
    /// 時間回復・広告回復・課金無限など、手段ごとに実装する。
    /// </summary>
    public interface IStaminaRecovery
    {
        /// <summary>回復を試みる。回復した量を返す（回復できなかった場合は 0）。</summary>
        UniTask<int> TryRecoverAsync();
    }
}
#nullable disable
