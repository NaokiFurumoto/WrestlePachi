#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// ステージ詳細パネルのスキル一覧、1件分のノード。
    /// SkillEntryNode.prefab にアタッチして使う。ScrollView の Content に動的生成する。
    /// </summary>
    public sealed class SkillEntryNode : MonoBehaviour
    {
        [SerializeField] private Image?    _icon;
        [SerializeField] private TMP_Text? _titleText;
        [SerializeField] private TMP_Text? _descriptionText;

        /// <summary>アイコン・タイトル（色付き）・説明文を一括設定する。icon が null ならアイコン非表示。</summary>
        public void Setup(Sprite? icon, string title, Color titleColor, string description)
        {
            if (_icon != null)
            {
                _icon.sprite  = icon;
                _icon.enabled = icon != null;
            }

            if (_titleText != null)
            {
                _titleText.text  = title;
                _titleText.color = titleColor;
            }

            if (_descriptionText != null)
                _descriptionText.text = description;
        }
    }
}
#nullable disable
