#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// ステージ選択グリッドの 1 セルを制御するコンポーネント。
    /// StageSelectNode.prefab にアタッチして使う。
    /// Setup() を呼ぶことでステージ番号・敵画像・状態表示を一括設定できる。
    /// </summary>
    public sealed class StageSelectNode : MonoBehaviour
    {
        [SerializeField] private TMP_Text?   _stageText;
        [SerializeField] private Image?      _enemyImage;
        [SerializeField] private GameObject? _clearGroup;
        [SerializeField] private GameObject? _nextGroup;
        [SerializeField] private GameObject? _lockGroup;
        [SerializeField] private Button?     _button;
        [SerializeField] private GameObject? _selectedGroup;

        public void Setup(
            int index,
            Sprite? enemySprite,
            StageSelectController.CellState state,
            Action<int> onTap)
        {
            if (_stageText != null)
                _stageText.text = $"ステージ{index + 1}";

            if (_enemyImage != null)
                _enemyImage.sprite = enemySprite;

            _clearGroup?.SetActive(state == StageSelectController.CellState.Cleared);
            _nextGroup?.SetActive(state  == StageSelectController.CellState.Next);
            _lockGroup?.SetActive(state  == StageSelectController.CellState.Locked);
            _selectedGroup?.SetActive(false);

            if (_button != null)
            {
                var i = index;
                _button.interactable = state != StageSelectController.CellState.Locked;
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onTap(i));
            }
        }

        public void SetSelected(bool selected) => _selectedGroup?.SetActive(selected);
    }
}
#nullable disable
