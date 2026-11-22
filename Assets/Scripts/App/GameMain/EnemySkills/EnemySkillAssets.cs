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
        [Tooltip("炎マスの背面表示スプライト")]
        [SerializeField] private Sprite? _fireTileSprite;

        [Header("UI：スキルアイコン（ステージ詳細画面のバッジ用・10種）")]
        [SerializeField] private SkillIconEntry[] _skillIcons = System.Array.Empty<SkillIconEntry>();

        public Sprite? RockHitSprite  => _rockHitSprite;
        public Sprite? BombPuyoSprite => _bombPuyoSprite;
        public Sprite? FireTileSprite => _fireTileSprite;

        /// <summary>指定スキルのアイコンを返す。未設定なら null。</summary>
        public Sprite? GetIcon(EnemySkillType type)
            => _skillIcons.FirstOrDefault(entry => entry.Type == type).Icon;
    }
}
#nullable disable
