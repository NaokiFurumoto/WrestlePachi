#nullable enable
namespace App
{
    /// <summary>ゲームプレイ中の一時停止が可能なオブジェクトを表すインターフェイス。</summary>
    public interface ISuspendable
    {
        void Suspend();
    }
}
#nullable disable
