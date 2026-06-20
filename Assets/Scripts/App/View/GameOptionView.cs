#nullable enable
using Cysharp.Threading.Tasks;
using GameSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace App
{
    /// <summary>
    /// ゲーム中オプション画面。
    /// BGM/SE 音量・スキルカットイン ON/OFF・スタミナ回復時間表示を提供する。
    /// GameMainController から PushViewAsync(ViewKeys.GAME_OPTION) で動的ロード。
    /// </summary>
    public sealed class GameOptionView : ViewBase
    {
        // ── 音量 ──────────────────────────────────────────────────────────
        [Header("音量")]
        [SerializeField] private Slider?   _bgmSlider;
        [SerializeField] private Slider?   _seSlider;

        // ── スタミナ ─────────────────────────────────────────────────────
        [Header("スタミナ")]
        [SerializeField] private TMP_Text? _staminaCurrentText;  // 現在値のみ
        [SerializeField] private TMP_Text? _staminaMaxText;      // 最大値のみ
        [SerializeField] private TMP_Text? _staminaRecoverText;  // 「00:00」の時間部分のみ

        // ── 設定 ─────────────────────────────────────────────────────────
        [Header("設定")]
        [SerializeField] private Toggle?   _cutInToggle;

        // ─────────────────────────────────────────────────────────────────

        protected override void OnInitialize()
        {
            // 音量スライダー初期値
            if (_bgmSlider != null)
                _bgmSlider.value = GameSettings.VolumeBGM;
            if (_seSlider != null)
                _seSlider.value = GameSettings.VolumeSE;

            // カットイントグル：コードで購読（Inspector接続不要）
            if (_cutInToggle != null)
            {
                _cutInToggle.SetIsOnWithoutNotify(GameSettings.CutInEnabled);
                _cutInToggle.onValueChanged.AddListener(OnCutInToggleChanged);
            }

            // スタミナ表示
            UpdateStaminaDisplay();
            if (StaminaManager.isValid)
                StaminaManager.Instance.OnChanged += OnStaminaChanged;
        }

        protected override void OnRelease()
        {
            if (_cutInToggle != null)
                _cutInToggle.onValueChanged.RemoveListener(OnCutInToggleChanged);
            if (StaminaManager.isValid)
                StaminaManager.Instance.OnChanged -= OnStaminaChanged;
        }

        private void OnStaminaChanged(int current, int max) => UpdateStaminaDisplay();

        private void UpdateStaminaDisplay()
        {
            if (!StaminaManager.isValid) return;
            var stamina = StaminaManager.Instance;

            if (_staminaCurrentText != null)
                _staminaCurrentText.text = stamina.Current.ToString();
            if (_staminaMaxText != null)
                _staminaMaxText.text = stamina.Max.ToString();

            // TODO: SaveManager 実装後に実際の回復残り時間を接続
            if (_staminaRecoverText != null)
                _staminaRecoverText.text = stamina.Current >= stamina.Max ? "--:--" : "-:--";
        }

        // ── ボタンコールバック（Inspector の onClick に登録） ──────────────
        // TODO: ロジックは PauseState / SceneManager 接続後に実装

        public void OnResumeClicked()      => ViewManager.PopView(this);
        public void OnRetryClicked()       => ConfirmAndTransitAsync(LocalizationKeys.Dialog.RETRY,        () => UnitySceneManager.GetActiveScene().name).Forget();
        public void OnTitleClicked()       => ConfirmAndTransitAsync(LocalizationKeys.Dialog.TITLE,        () => "BootScene").Forget();
        public void OnStageSelectClicked() => ConfirmAndTransitAsync(LocalizationKeys.Dialog.STAGE_SELECT, () => "StageSelectScene").Forget();

        private async UniTaskVoid ConfirmAndTransitAsync(string key, System.Func<string> sceneNameGetter)
        {
            var yes = await DialogView.ShowAsync(key, destroyCancellationToken);
            if (!yes) return;
            if (SceneManager.isValid)
                SceneManager.Instance.TransitScene(sceneNameGetter());
        }

        // ── スライダーコールバック（Inspector の onValueChanged に登録） ──
        // TODO: SaveManager 実装後に永続化を追加

        public void OnBgmVolumeChanged(float value)
        {
            GameSettings.VolumeBGM = value;
            AppSound.SetBGMVolume(value);
        }

        public void OnSeVolumeChanged(float value)
        {
            GameSettings.VolumeSE = value;
        }

        // ── トグルコールバック（Inspector の onValueChanged に登録） ──────
        // TODO: SaveManager 実装後に永続化を追加

        public void OnCutInToggleChanged(bool isOn) => GameSettings.CutInEnabled = isOn;
    }
}
#nullable disable
