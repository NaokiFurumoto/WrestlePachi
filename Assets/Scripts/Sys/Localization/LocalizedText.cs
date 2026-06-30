using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameSys
{
    /// <summary>
    /// UIテキスト用ローカライズ。
    /// キーのみの場合はそのまま表示。SetFormat() でフォーマット引数を渡すと string.Format を適用する。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("key")] private string _key = "";

        private TMP_Text?  _text;
        private object[]?  _args;

        private TMP_Text Text => _text != null ? _text : (_text = GetComponent<TMP_Text>());

        private void OnEnable()
            => Refresh();

        /// <summary>
        /// フォーマット引数を設定してテキストを更新する。
        /// 例: SetFormat(stageNumber) → "Stage {0}" → "Stage 1"
        /// </summary>
        public void SetFormat(params object[] args)
        {
            _args = args;
            Refresh();
        }

        /// <summary>現在の言語でテキストを再描画する（言語切り替え時などに外から呼ぶ）。</summary>
        public void Refresh()
        {
            if (!LocalizationManager.isValid) return;

            var raw = LocalizationManager.Instance.GetText(_key);
            Text.text = (_args != null && _args.Length > 0)
                ? string.Format(raw, _args)
                : raw;
        }
    }
}
