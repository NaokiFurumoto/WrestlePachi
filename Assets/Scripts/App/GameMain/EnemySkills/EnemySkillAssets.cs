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
        [Tooltip("上から落ちてくる岩（砕けたときの破片にも使う）")]
        [SerializeField] private Sprite? _rockFallSprite;
        [Tooltip("ぷよにかぶせる岩の殻（中央がくり抜かれていて、ぷよの色が見える）")]
        [SerializeField] private Sprite? _rockShellSprite;
        [Tooltip("1回巻き込まれた後のヒビ入りの殻（普通の殻と同じ位置・大きさ）")]
        [SerializeField] private Sprite? _rockShellCrackSprite;
        [Tooltip("殻の絵がないときに、1回目のヒットで差し替えるぷよのスプライト（旧仕様）")]
        [FormerlySerializedAs("_pineappleHitSprite")]
        [SerializeField] private Sprite? _rockHitSprite;

        [Header("盤面演出：時限爆弾")]
        [Tooltip("爆弾ぷよのスプライト（基準点を丸い本体の中心に合わせてある）")]
        [SerializeField] private Sprite? _bombPuyoSprite;
        [Tooltip("爆発の絵")]
        [SerializeField] private Sprite? _bombExplosionSprite;
        [Tooltip("爆弾の絵の中の導火線の先端の位置（左下0・右上1の割合）。火花をここに出す")]
        [SerializeField] private Vector2 _bombFuseTip = new(0.614f, 0.797f);

        [Header("盤面演出：炎")]
        [Tooltip("炎マスで燃え続ける炎のスプライト")]
        [SerializeField] private Sprite? _fireTileSprite;
        [Tooltip("炎マスに落ちてくる火の玉のスプライト")]
        [SerializeField] private Sprite? _fireBallSprite;
        [Tooltip("炎マスの炎を揺らめかせるマテリアル（App/FireFlicker シェーダー）。未設定なら通常描画")]
        [SerializeField] private Material? _fireTileMaterial;

        [Header("盤面演出：サメ")]
        [Tooltip("ぷよに食いつく瞬間の、口を開けたサメ（右向き）")]
        [SerializeField] private Sprite? _sharkOpenSprite;
        [Tooltip("泳いでいるときの、口を閉じたサメ（右向き・口を開けた絵と体の位置を合わせてある）")]
        [SerializeField] private Sprite? _sharkClosedSprite;

        [Header("UI：スキルアイコン（ステージ詳細画面のバッジ用・10種）")]
        [SerializeField] private SkillIconEntry[] _skillIcons = System.Array.Empty<SkillIconEntry>();

        public Sprite? RockFallSprite       => _rockFallSprite;
        public Sprite? RockShellSprite      => _rockShellSprite;
        public Sprite? RockShellCrackSprite => _rockShellCrackSprite;
        public Sprite? RockHitSprite        => _rockHitSprite;
        public Sprite? BombPuyoSprite      => _bombPuyoSprite;
        public Sprite? BombExplosionSprite => _bombExplosionSprite;
        public Vector2 BombFuseTip         => _bombFuseTip;
        public Sprite? FireTileSprite => _fireTileSprite;
        public Sprite? FireBallSprite => _fireBallSprite;
        public Material? FireTileMaterial => _fireTileMaterial;
        public Sprite? SharkOpenSprite   => _sharkOpenSprite;
        public Sprite? SharkClosedSprite => _sharkClosedSprite;

        /// <summary>指定スキルのアイコンを返す。未設定なら null。</summary>
        public Sprite? GetIcon(EnemySkillType type)
            => _skillIcons.FirstOrDefault(entry => entry.Type == type).Icon;
    }
}
#nullable disable
