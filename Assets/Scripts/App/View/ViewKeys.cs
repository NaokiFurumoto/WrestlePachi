namespace App
{
    /// <summary>
    /// ViewManager で動的ロードする View の Prefab パスを一元管理するクラス。
    /// PrefabManager は Resources フォルダ相対パスで検索する。
    /// </summary>
    public static class ViewKeys
    {
        public const string PUYO_INPUT    = "Prefabs/Views/PiyoInputView";
        public const string SKILL_CUT_IN  = "Prefabs/Views/SkillCutinView";
        public const string SKILL_RAINBOW = "Prefabs/Views/SkillRainbowView";
        public const string GAME_CLEAR    = "Prefabs/Views/GameClearView";
        public const string GAME_OPTION   = "Prefabs/Views/GameOptionView";
        public const string DIALOG        = "Prefabs/Views/DialogView";
        public const string GAME_OVER          = "Prefabs/Views/GameOverView";

        // ─── StageSelect ──────────────────────────────────────────
        public const string STAGE_SELECT_NODE  = "Prefabs/Node/StageSelectNode";
        public const string STAGE_SELECT_VIEW   = "Prefabs/Views/StageSelectView";
        public const string STAGE_DETAIL       = "Prefabs/Views/StageDetailPanel";
    }
}
