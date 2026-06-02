using UnityEditor;

namespace App.EditorTools
{
    /// <summary>
    /// デバッグウィンドウの各セクションが実装するインターフェース。
    /// このインターフェースを実装するだけで WrestlePachiDebugWindow に自動登録される。
    /// WrestlePachiDebugWindow への登録コードは一切不要。
    /// </summary>
    public interface IDebugSection
    {
        /// <summary>セクションのヘッダー名（折りたたみタイトル）</summary>
        string Title { get; }

        /// <summary>表示順（小さい値が上）</summary>
        int Order { get; }

        /// <summary>セクションの GUI を描画する。Play 中のみ呼ばれる。</summary>
        void OnGUI(GameMainController ctrl);
    }
}
