namespace App
{
    /// <summary>
    /// ローカライズキー定数。LocalizationDatabase の key 列と一致させる。
    /// </summary>
    public static class LocalizationKeys
    {
        /// <summary>複数画面で共通して使うラベル</summary>
        public static class Common
        {
            public const string STAMINA       = "common.stamina";
            public const string RETRY         = "common.retry";
            public const string TITLE         = "common.title";
            public const string STAGE_SELECT  = "common.stage_select";
            public const string YES           = "common.yes";
            public const string NO            = "common.no";
            public const string NEXT          = "common.next";
            public const string ROUND         = "common.round";
        }

        /// <summary>HUD</summary>
        public static class Hud
        {
            public const string STAGE         = "hud.stage";
            public const string TIMELIMIT     = "hud.timelimit";
        }

        /// <summary>オプション画面</summary>
        public static class Option
        {
            public const string RESUME        = "option.resume";
            public const string RECOVER_TIMER = "option.recover_timer";
            public const string SKILL_CUTIN   = "option.skill_cutin";
        }

        /// <summary>ゲームクリア画面</summary>
        public static class GameClear
        {
            public const string NEXT_BATTLE   = "gameclear.next_battle";
            public const string STAGE         = "gameclear.stage";
            public const string DEFEATED      = "gameclear.defeated";
        }

        /// <summary>ゲームオーバー画面</summary>
        public static class GameOver
        {
            public const string STAGE          = "gameover.stage";          // 「ステージ{0}」
            public const string DEFEATED      = "gameover.defeated";      // 「{0}に敗北!!」
            public const string STAMINA_LABEL  = "gameover.stamina_label";  // 「残りスタミナ」ラベル
            public const string STAMINA        = "gameover.stamina";        // 「{0}/{1}個」
            public const string RECOVERY_LABEL = "gameover.recovery_label"; // 「スタミナ回復まで」ラベル
        }

        /// <summary>ダイアログ</summary>
        public static class Dialog
        {
            public const string CONFIRM      = "dlg.confirm";
            public const string RETRY        = "dlg.retry";
            public const string TITLE        = "dlg.title";
            public const string STAGE_SELECT = "dlg.stage_select";
        }
    }
}
