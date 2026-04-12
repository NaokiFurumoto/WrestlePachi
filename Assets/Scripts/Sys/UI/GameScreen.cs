#nullable enable
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

namespace GameSys
{
    /// <summary>
    /// ゲーム画面関連
    /// </summary>
    public static class GameScreen
    {
        const int           UI_CENTER_WIDTH         = 1080;
        const int           UI_CENTER_HEIGHT        = 1920;

        // 背景のベース解像度(16:9)
        const int           UI_BACKGROUND_WIDTH     = 1080;
        const int           UI_BACKGROUND_HEIGHT    = 1920;
       

        private static      int     m_Width;
        private static      int     m_Height;
        private static      int     m_UiCenterWidth;
        private static      int     m_UiCenterHeight;
        private static      int     m_UiCenterOffsetY;
        private static      float   m_UiCenterScale;
        private static      float   m_UiBackgroundScale;
        
        public static int   Width                   => m_Width;
        public static int   Height                  => m_Height;
        public static int   UiCenterWidth           => m_UiCenterWidth;
        public static int   UiCenterHeight          => m_UiCenterHeight;
        public static int   UiCenterOffsetY         => m_UiCenterOffsetY;
        public static float UiCenterScale           => m_UiCenterScale;
        public static float UiBackgroundScale       => m_UiBackgroundScale;

        /// <summary>
        /// アプリ起動時、シーンがロードされる前に自動で実行される
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            CalcScreenSize();
        }

        /// <summary>
        /// スクリーンサイズから、各パラメータの計算
        /// </summary>
        public static void CalcScreenSize()
        {
            m_Width     = Screen.width;
            m_Height    = Screen.height;
            
            _CalcCanvasScale();
        }

        /// <summary>
        /// CanvasScaleの値を計算
        /// </summary>
        private static void _CalcCanvasScale()
        {
            if (m_Width == 0 || m_Height == 0)
                return;

            var safeArea = Screen.safeArea;

            var safeH = safeArea.height;
            var safeW = safeArea.width;

            float sclW = safeW / UI_CENTER_WIDTH;
            float sclH = safeH / UI_CENTER_HEIGHT;

            // 小さい方を採用
            m_UiCenterScale = Mathf.Min(sclW, sclH);

            m_UiCenterWidth = Mathf.CeilToInt(UI_CENTER_WIDTH * (sclW / m_UiCenterScale));
            m_UiCenterHeight = Mathf.CeilToInt(UI_CENTER_HEIGHT * (sclH / m_UiCenterScale));
            m_UiCenterOffsetY = Mathf.CeilToInt((safeArea.y * 0.5f) / m_UiCenterScale);

            float bgSclW = m_Width / (float)UI_BACKGROUND_WIDTH;
            float bgSclH = m_Height / (float)UI_BACKGROUND_HEIGHT;

            m_UiBackgroundScale = Mathf.Max(bgSclW, bgSclH);
        }
    }

    /// <summary>
    /// SortOrder定義
    /// </summary>
    public static class SortOrderConst
    {
        public  const   int         SORTING_ORDER_SCENE_FADE    = 30000;        // SceneFadeのSortOrder
        public  const   int         SORTING_ORDER_CONNECTING    = 30100;        // 通信中
        
        public  const   int         SORTING_ORDER_PER_VIEW      = 10;           // 1ViewごとのSortingOrder区切り
    }
}

#nullable disable
