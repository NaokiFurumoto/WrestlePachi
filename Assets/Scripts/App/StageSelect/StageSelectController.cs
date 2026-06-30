#nullable enable
using System;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;

namespace App
{
    /// <summary>
    /// ステージ選択ロジックを担うコントローラー。
    /// セル状態の算出・詳細パネルの表示・ゲームシーンへの遷移を管理する。
    /// </summary>
    public sealed class StageSelectController : MonoBehaviour
    {
        public enum CellState { Locked, Normal, Next, Cleared }

        private const string KeyClearPrefix = "stage_clear_";

        private ViewManager? _viewMng;

        public Action? OnDetailClosed { get; set; }

        public void Initialize(ViewManager? viewMng)
        {
            _viewMng = viewMng;
        }

        // ─── セル状態 ────────────────────────────────────────────

        /// <summary>
        /// 指定インデックスのセル状態を返す。
        /// ロック条件：前ステージが未クリア。Next：最初のアンロック済み未クリアステージ。
        /// </summary>
        public CellState GetCellState(int index)
        {
            if (index > 0 && !IsCleared(index - 1)) return CellState.Locked;
            if (IsCleared(index))                   return CellState.Cleared;
            if (GetNextStageIndex() == index)        return CellState.Next;
            return CellState.Normal;
        }

        public int GetNextStageIndex()
        {
            var count = StageConfigList.Instance?.Count ?? 0;
            for (var i = 0; i < count; i++)
            {
                if (!IsCleared(i)) return i;
            }
            return Mathf.Max(0, count - 1);
        }

        public bool IsCleared(int index) => PlayerPrefs.GetInt(KeyClearPrefix + index, 0) == 1;

        public void MarkCleared(int index)
        {
            PlayerPrefs.SetInt(KeyClearPrefix + index, 1);
            PlayerPrefs.Save();
        }

        // ─── ユーザー操作 ────────────────────────────────────────

        public void OnCellTapped(int index)
        {
            if (GetCellState(index) == CellState.Locked) return;

            var config = StageConfigList.Instance?.Get(index);
            if (config == null) return;

            var data = new StageDetailPanel.StageDetailData
            {
                StageIndex  = index,
                Config      = config,
                OnChallenge = () => Challenge(index),
                OnClosed    = OnDetailClosed,
            };
            ViewManager.PushViewAsync<StageDetailPanel>(ViewKeys.STAGE_DETAIL, data).Forget();
        }

        private void Challenge(int index)
        {
            GameSettings.CurrentStageIndex = index;
            if (SceneManager.isValid)
                SceneManager.Instance.TransitScene("GameScene");
        }
    }
}
#nullable disable
