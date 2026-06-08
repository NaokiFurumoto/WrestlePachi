using UnityEngine;

namespace App
{
    /// <summary>
    /// ゲームモードごとのパラメーター設定。
    /// ScriptableObject として Assets に作成し、GameMainController にアサインする。
    /// </summary>
    [CreateAssetMenu(fileName = "GameModeConfig", menuName = "WrestlePachi/GameModeConfig")]
    public sealed class GameModeConfig : ScriptableObject
    {
        // ─── 玉発射 ───────────────────────────────────────────────
        [Header("玉発射")]
        [Tooltip("消えたぷよ1個あたりの発射球数。1.0=1個→1球、0.5=2個→1球")]
        [SerializeField, Min(0f)] private float _ballsPerPuyo = 1f;

        // ─── ダメージ ─────────────────────────────────────────────
        [Header("ダメージ")]
        [Tooltip("通常連鎖ダメージの基礎値（消えたぷよ1個あたり）")]
        [SerializeField, Min(0)] private int _chainBaseDamage = 3;

        // ─── 保留 ────────────────────────────────────────────────
        [Header("保留")]
        [Tooltip("虹保留確率の分母（20=1/20、50=1/50）。0にすると虹保留なし")]
        [SerializeField, Min(0)] private int _rainbowProbabilityDenominator = 50;

        [Tooltip("Black保留が出る確率。0=出ない、1=必ずBlack。ステージ難易度で調整する")]
        [SerializeField, Range(0f, 1f)] private float _blackHoldProbability = 0.1f;

        [Tooltip("黒保留：1回の技発動で降らせるお邪魔ぷよの数（6=1列分）")]
        [SerializeField, Min(1)] private int _ojamaCountPerBlackTech = 6;

        [Tooltip("保留・ぷよの色バリアント数。4=Red〜Blue、5=Red〜Purple")]
        [SerializeField, Range(4, 5)] private int _colorVariant = 4;

        /// <summary>通常連鎖ダメージの基礎値</summary>
        public int ChainBaseDamage => _chainBaseDamage;

        /// <summary>消えたぷよ数から発射球数を計算する（最低1球）</summary>
        public int CalcBallCount(int clearedPuyoCount)
            => Mathf.Max(1, Mathf.RoundToInt(clearedPuyoCount * _ballsPerPuyo));

        /// <summary>虹保留確率の分母。0 なら虹保留なし</summary>
        public int RainbowProbabilityDenominator => _rainbowProbabilityDenominator;

        /// <summary>Black保留が出る確率（0〜1）</summary>
        public float BlackHoldProbability => _blackHoldProbability;

        /// <summary>黒保留1発動あたりのお邪魔ぷよ数</summary>
        public int OjamaCountPerBlackTech => _ojamaCountPerBlackTech;

        /// <summary>保留・ぷよの色バリアント数（4=Red〜Blue、5=Red〜Purple）</summary>
        public int ColorVariant => _colorVariant;

#if UNITY_EDITOR
        public float Debug_BallsPerPuyo => _ballsPerPuyo;
        public void Debug_SetBlackHoldProbability(float value)         => _blackHoldProbability          = Mathf.Clamp01(value);
        public void Debug_SetBallsPerPuyo(float value)                => _ballsPerPuyo                  = Mathf.Max(0f, value);
        public void Debug_SetOjamaCountPerBlackTech(int value)        => _ojamaCountPerBlackTech         = Mathf.Max(1, value);
        public void Debug_SetColorVariant(int value)                  => _colorVariant                  = Mathf.Clamp(value, 4, 5);
        public void Debug_SetRainbowProbabilityDenominator(int value) => _rainbowProbabilityDenominator  = Mathf.Max(0, value);
#endif
    }
}
