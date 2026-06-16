#nullable enable
using GameSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private TMP_Text? _staminaValueText;    // 「X / Y」
        [SerializeField] private TMP_Text? _staminaRecoverText;  // 「次回回復まで X:XX」

        // ── 設定 ─────────────────────────────────────────────────────────
        [Header("設定")]
        [SerializeField] private Toggle?   _cutInToggle;

        // ── ボタン ───────────────────────────────────────────────────────
        [Header("ボタン")]
        [SerializeField] private Button?   _resumeButton;
        [SerializeField] private Button?   _retryButton;
        [SerializeField] private Button?   _titleButton;
        [SerializeField] private Button?   _stageSelectButton;

        // ─────────────────────────────────────────────────────────────────

        protected override void OnInitialize()
        {
            // 音量スライダー初期値
            if (_bgmSlider != null && SoundManager.isValid)
                _bgmSlider.value = SoundManager.Instance.VolumeBGM;
            if (_seSlider != null && SoundManager.isValid)
                _seSlider.value = SoundManager.Instance.VolumeSE;

            // スタミナ表示
            UpdateStaminaDisplay();
            if (StaminaManager.isValid)
                StaminaManager.Instance.OnChanged += OnStaminaChanged;
        }

        protected override void OnRelease()
        {
            if (StaminaManager.isValid)
                StaminaManager.Instance.OnChanged -= OnStaminaChanged;
        }

        private void OnStaminaChanged(int current, int max) => UpdateStaminaDisplay();

        private void UpdateStaminaDisplay()
        {
            if (!StaminaManager.isValid) return;
            var stamina = StaminaManager.Instance;

            if (_staminaValueText != null)
                _staminaValueText.text = $"{stamina.Current} / {stamina.Max}";

            // TODO: SaveManager 実装後に実際の回復残り時間を接続
            if (_staminaRecoverText != null)
                _staminaRecoverText.text = stamina.Current >= stamina.Max
                    ? "スタミナ満タン"
                    : "次回回復まで -:--";
        }

        // ── ボタンコールバック（Inspector の onClick に登録） ──────────────
        // TODO: ロジックは PauseState / SceneManager 接続後に実装

        public void OnResumeClicked()      { /* TODO: ViewManager.PopViewAsync + TimeManager.Resume() */ }
        public void OnRetryClicked()       { /* TODO: SceneManager 経由で現ステージ再ロード */ }
        public void OnTitleClicked()       { /* TODO: SceneManager.Instance.TransitScene("BootScene") */ }
        public void OnStageSelectClicked() { /* TODO: SceneManager 経由でステージ選択シーンへ */ }

        // ── スライダーコールバック（Inspector の onValueChanged に登録） ──
        // TODO: SaveManager 実装後に永続化を追加

        public void OnBgmVolumeChanged(float value)
        {
            if (SoundManager.isValid) SoundManager.Instance.SetVolumeBGM(value);
        }

        public void OnSeVolumeChanged(float value)
        {
            if (SoundManager.isValid) SoundManager.Instance.SetVolumeSE(value);
        }

        // ── トグルコールバック（Inspector の onValueChanged に登録） ──────
        // TODO: SaveManager 実装後に永続化を追加

        public void OnCutInToggleChanged(bool isOn) { /* TODO: カットイン表示フラグを保存して参照させる */ }
    }
}
#nullable disable
