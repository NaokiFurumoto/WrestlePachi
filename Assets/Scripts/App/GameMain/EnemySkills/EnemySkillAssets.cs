#nullable enable
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace App
{
    /// <summary>
    /// 敵スキルが必要とするスプライト等のアセット参照をまとめた ScriptableObject。
    /// GameModeConfig と同様、Assets に作成して GameMainController にアサインする。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySkillAssets", menuName = "WrestlePachi/EnemySkillAssets")]
    public sealed class EnemySkillAssets : ScriptableObject
    {
        [Header("盤面演出：岩")]
        [Tooltip("1回目のヒットで差し替えるスプライト")]
        [FormerlySerializedAs("_pineappleHitSprite")]
        [SerializeField] private Sprite? _rockHitSprite;

        [Header("盤面演出：時限爆弾")]
        [Tooltip("爆弾ぷよのスプライト")]
        [SerializeField] private Sprite? _bombPuyoSprite;

        [Header("盤面演出：炎")]
        [Tooltip("炎マスで燃え続ける炎のスプライト")]
        [SerializeField] private Sprite? _fireTileSprite;
        [Tooltip("炎マスに落ちてくる火の玉のスプライト")]
        [SerializeField] private Sprite? _fireBallSprite;

        [Header("盤面演出：サメ")]
        [Tooltip("ぷよに食いつく瞬間の、口を開けたサメ（右向き）")]
        [SerializeField] private Sprite? _sharkOpenSprite;
        [Tooltip("泳いでいるときの、口を閉じたサメ（右向き・口を開けた絵と体の位置を合わせてある）")]
        [SerializeField] private Sprite? _sharkClosedSprite;

        [Header("UI：スキルアイコン（ステージ詳細画面のバッジ用・10種）")]
        [SerializeField] private SkillIconEntry[] _skillIcons = System.Array.Empty<SkillIconEntry>();

        public Sprite? RockHitSprite  => _rockHitSprite;
        public Sprite? BombPuyoSprite => _bombPuyoSprite;
        public Sprite? FireTileSprite => _fireTileSprite;
        public Sprite? FireBallSprite => _fireBallSprite;
        public Sprite? SharkOpenSprite   => _sharkOpenSprite;
        public Sprite? SharkClosedSprite => _sharkClosedSprite;

        /// <summary>指定スキルのアイコンを返す。未設定なら null。</summary>
        public Sprite? GetIcon(EnemySkillType type)
            => _skillIcons.FirstOrDefault(entry => entry.Type == type).Icon;
    }
}
#nullable disable
