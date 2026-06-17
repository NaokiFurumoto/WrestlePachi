#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using GameSys;
using TMPro;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 汎用 Yes/No ダイアログ。
    /// DialogView.ShowAsync(LocalizationKeys.Dialog.RETRY, ct) を await するだけで結果が取れる。
    /// はい/いいえ ボタンのラベルは Prefab 側の LocalizedText コンポーネントで管理。
    /// </summary>
    public sealed class DialogView : ViewBase
    {
        [SerializeField] private TMP_Text? _messageText;

        private readonly UniTaskCompletionSource<bool> _tcs = new();

        // ── 外部から呼ぶ ─────────────────────────────────────────────────

        /// <summary>ダイアログを表示して Yes=true / No=false を返す。</summary>
        public static async UniTask<bool> ShowAsync(string key, CancellationToken ct)
        {
            var handle = await ViewManager.PushViewAsync<DialogView>(ViewKeys.DIALOG);
            if (handle?.View is not DialogView view) return false;
            view.Setup(key);
            return await view._tcs.Task.AttachExternalCancellation(ct);
        }

        // ── ボタンコールバック（Inspector の onClick に登録） ─────────────

        public void OnYesClicked()
        {
            _tcs.TrySetResult(true);
            ViewManager.PopView(this);
        }

        public void OnNoClicked()
        {
            _tcs.TrySetResult(false);
            ViewManager.PopView(this);
        }

        // ── 内部 ─────────────────────────────────────────────────────────

        private void Setup(string key)
        {
            if (_messageText == null) return;
            _messageText.text = LocalizationManager.isValid
                ? LocalizationManager.Instance.GetText(key)
                : key;
        }

        protected override void OnRelease()
        {
            // ESC やシーン破棄で強制クローズされた場合は No 扱い
            _tcs.TrySetResult(false);
        }
    }
}
#nullable disable
