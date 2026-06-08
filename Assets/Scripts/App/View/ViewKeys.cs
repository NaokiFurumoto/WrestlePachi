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
    }
}
