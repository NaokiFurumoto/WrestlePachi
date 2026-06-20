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
        public sealed class DialogViewData : ViewData
        {
            public string Key { get; set; } = "";
        }

        [SerializeField] private TMP_Text? _messageText;

        private readonly UniTaskCompletionSource<bool> _tcs = new();

        // ── 外部から呼ぶ ─────────────────────────────────────────────────

        /// <summary>ダイアログを表示して Yes=true / No=false を返す。</summary>
        public static async UniTask<bool> ShowAsync(string key, CancellationToken ct)
        {
            var data   = new DialogViewData { Key = key };
            var handle = await ViewManager.PushViewAsync<DialogView>(ViewKeys.DIALOG, data);
            if (handle?.View is not DialogView view) return false;
            return await view._tcs.Task.AttachExternalCancellation(ct);
        }

        // ── 初期化（オープンアニメ前にテキストをセット） ─────────────────

        protected override void OnInitialize()
        {
            if (Data is not DialogViewData data) return;
            if (_messageText == null) return;
            _messageText.text = LocalizationManager.isValid
                ? LocalizationManager.Instance.GetText(data.Key)
                : data.Key;
        }

        // ── ボタンコールバック（Inspector の onClick に登録） ─────────────

        public void OnYesClicked() => CompleteAsync(true).Forget();
        public void OnNoClicked()  => CompleteAsync(false).Forget();

        // ── 内部 ─────────────────────────────────────────────────────────

        private async UniTask CompleteAsync(bool result)
        {
            _tcs.TrySetResult(result);
            await PopAsync();
        }

        protected override void OnRelease()
        {
            // ESC やシーン破棄で強制クローズされた場合は No 扱い
            _tcs.TrySetResult(false);
        }
    }
}
#nullable disable
