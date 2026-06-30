#nullable enable
using UnityEngine;

namespace GameSys
{
    [CreateAssetMenu(fileName = "AppSoundData", menuName = "WrestlePachi/AppSoundData")]
    public sealed class AppSoundData : ScriptableObject
    {
        [Header("BGM")]
        public AudioClip? bgmGame;
        public AudioClip? bgmClear;
        public AudioClip? bgmGameOver;

        [Header("SE - UI")]
        public AudioClip? seBtnOk;
        public AudioClip? seBtnCancel;
        public AudioClip? seOpenWindow;
        public AudioClip? seTap;

        [Header("SE - ゲーム進行")]
        public AudioClip? seGong;
        public AudioClip? seGameClear;
        public AudioClip? seGameOver;
        public AudioClip? seTimer;

        [Header("SE - ぷよ操作")]
        public AudioClip? sePuyoMove;
        public AudioClip? sePuyoRotate;
        public AudioClip? sePuyoLand;
        public AudioClip? seOjamaLand;
        public AudioClip? sePuyoFlash;
        public AudioClip? sePuyoClear;
        public AudioClip? seChain;

        [Header("SE - パチンコ")]
        public AudioClip? seHesoIn;
        public AudioClip? seBallLaunch;
        public AudioClip? seBallNailHit;
        public AudioClip? seHoldAdd;

        [Header("SE - スキル・戦闘")]
        public AudioClip? seCutIn;
        public AudioClip? seRainbowCutIn;
        public AudioClip? seRainbowVibration;
        public AudioClip? seRainbowView;
        public AudioClip? seDamageHit;
        public AudioClip? seEnemyDefeat;
        public AudioClip? seBom;

        [Header("SE - スキルエフェクト（カットイン後）")]
        public AudioClip? seSkillDropKick;
        public AudioClip? seSkillHipDrop;
        public AudioClip? seSkillStomping;
        public AudioClip? seSkillLariat;
        public AudioClip? seSkillTackle;
        public AudioClip? seSkillAllClear;
    }
}
#nullable disable
