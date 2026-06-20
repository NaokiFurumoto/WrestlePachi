using UnityEngine;

namespace App
{
    /// <summary>
    /// PlayerPrefs に保存するゲーム設定の一元管理。
    /// </summary>
    public static class GameSettings
    {
        private const string KeyCutInEnabled = "CutInEnabled";
        private const string KeyVolumeBGM    = "VolumeBGM";
        private const string KeyVolumeSE     = "VolumeSE";

        public static bool CutInEnabled
        {
            get => PlayerPrefs.GetInt(KeyCutInEnabled, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(KeyCutInEnabled, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static float VolumeBGM
        {
            get => PlayerPrefs.GetFloat(KeyVolumeBGM, 1f);
            set
            {
                PlayerPrefs.SetFloat(KeyVolumeBGM, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        public static float VolumeSE
        {
            get => PlayerPrefs.GetFloat(KeyVolumeSE, 1f);
            set
            {
                PlayerPrefs.SetFloat(KeyVolumeSE, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }
    }
}
