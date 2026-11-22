#nullable enable
namespace App
{
    /// <summary>
    /// 常時効果型の敵スキル基底クラス。
    /// ステージ開始時に状態をONにし、終了時にOFFにするだけの単純な生存期間管理を行う。
    /// 実際の効果は OnEnter でイベント購読・フラグ設定を行う形で各スキルが実装する。
    /// </summary>
    public abstract class EnemyPassiveSkillBase : IEnemySkill
    {
        public abstract EnemySkillType Type { get; }

        /// <summary>スキルレベル（1始まり）。高いほど効果範囲・強度が上がる</summary>
        public int Level { get; }

        protected EnemyPassiveSkillBase(int level)
        {
            Level = level;
        }

        /// <summary>ステージ開始時に呼ばれる。状態ON・イベント購読を行う</summary>
        public abstract void OnEnter(GameContext ctx);

        /// <summary>ステージ終了・敵切替時に呼ばれる。状態OFF・購読解除を行う</summary>
        public abstract void OnRelease(GameContext ctx);
    }
}
#nullable disable
