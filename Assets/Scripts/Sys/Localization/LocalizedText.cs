using TMPro;
using UnityEngine;
using System;
namespace GameSys
{
    /// <summary>
    /// UIテキスト用ローカライズ
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField]
        private string key;

        private TMP_Text textComponent;

        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            UpdateText();
        }

        /// <summary>
        /// テキスト更新
        /// </summary>
        public void UpdateText()
        {
            if (!LocalizationManager.isValid)
                return;

            textComponent.text =
                LocalizationManager.Instance.GetText(key);
        }
    }
}


