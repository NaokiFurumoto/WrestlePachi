#nullable enable
using TMPro;

namespace GameSys
{
    /// <summary>
    /// TextMeshProのExtension
    /// </summary>
    public static class TextMeshProExtension
    {
        /// <summary>
        /// テキスト変更
        /// </summary>
        public static void SetTextNullable( this TextMeshProUGUI? tmp, string text)
        {
            if(tmp != null)
            {
                tmp.SetText(text);
            }
        }
    }
}

#nullable disable
