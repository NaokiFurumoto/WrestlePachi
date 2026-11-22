#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// ステージのクリア進行状況を管理する静的ユーティリティ。
    /// PlayerPrefs に永続化するため、GameScene・StageScene のどちらからでも
    /// インスタンス参照なしに呼べる（StageSelectController は StageScene にしか存在しないため、
    /// GameMainController からクリアフラグを立てるにはこの形が必要）。
    /// </summary>
    public static class StageProgress
    {
        private const string KeyClearPrefix = "stage_clear_";

        /// <summary>指定インデックスのステージがクリア済みかどうかを返す。</summary>
        public static bool IsCleared(int index) => PlayerPrefs.GetInt(KeyClearPrefix + index, 0) == 1;

        /// <summary>指定インデックスのステージをクリア済みにする。GameMainController.OnEnemyDefeated から呼ぶ。</summary>
        public static void MarkCleared(int index)
        {
            PlayerPrefs.SetInt(KeyClearPrefix + index, 1);
            PlayerPrefs.Save();
        }
    }
}
#nullable disable
