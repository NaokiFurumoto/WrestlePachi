#nullable enable
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// ゲーム固有サウンドの App 層ファサード。
    /// GameSys.SoundManager を介して SE / BGM をキー名で再生する。
    /// シーン上に配置、または GameMainScene.BootstrapAsync で自動生成される。
    /// </summary>
    public sealed class AppSound : BehaviourSingleton<AppSound>
    {
        // ─── クランプキー（Resources/Sound/SE/ 以下のアセット名）──────
        public const string SE_CLUMP = "CommonSE";

        // ─── SE キー定数 ─────────────────────────────────────────────
        // キーは SoundClump の自動生成ルール（ファイル名を大文字化・SE_プレフィックス除去）に合わせる
        public static class SE
        {
            public const string Allow       = "ALLOW";
            public const string BtnCancel   = "BTNCANCEL";
            public const string BtnOk       = "BTNOK";
            public const string Chain       = "CHAINSE";
            public const string ClearGame   = "CLEARGAME";
            public const string Gong        = "GONG_SE";
            public const string OpenWindow  = "OPENWINDOW";
            public const string Rotate      = "ROTATE";
            public const string StartVoice  = "STARTTAP";
            public const string Tap         = "TAP";
            public const string BtnUpDown   = "UPDOWN";
            public const string Delete      = "DELETE";
            public const string Bom         = "BOM";
            public const string ChainBlue   = "CUTBLUE";
            public const string ChainGreen  = "CUTGREEN";
            public const string ChainYellow = "CUTYELLOW";
            public const string ChainRed    = "CUTRED";
            public const string ChainAll    = "CUTALL";
            public const string Timer       = "TIMER";
        }

        // ─── BehaviourSingleton ───────────────────────────────────────
        protected override void _SetInstance() => s_Instance = this;

        private void Start()
        {
            // Awake より後に呼ばれるため SoundManager の初期化完了が保証される
            if( SoundManager.isValid )
                SoundManager.Instance.Initialize( SE_CLUMP );
        }

        // ─── 静的 API ─────────────────────────────────────────────────
        public static void PlayGong()        => Play( SE.Gong );
        public static void PlayClearGame()   => Play( SE.ClearGame );
        public static void PlayChain()       => Play( SE.Chain );
        public static void PlayBtnOk()       => Play( SE.BtnOk );
        public static void PlayBtnCancel()   => Play( SE.BtnCancel );
        public static void PlayTap()         => Play( SE.Tap );
        public static void PlayTimer()       => Play( SE.Timer );
        public static void PlayBom()         => Play( SE.Bom );

        public static void FadeOutBGM( float duration = 2f )
        {
            if( SoundManager.isValid )
                SoundManager.Instance.StopBGM( duration );
        }

        private static void Play( string key )
        {
            if( SoundManager.isValid )
                SoundManager.PlayCommonSE( key );
        }
    }
}
#nullable disable
