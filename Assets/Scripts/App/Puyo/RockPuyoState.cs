#nullable enable
using DG.Tweening;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>岩ぷよの見た目に使う絵。どれも未設定なら灰色に染める仮表示になる</summary>
    public readonly struct RockVisuals
    {
        /// <summary>ぷよにかぶせる岩の殻（中央がくり抜かれていて、下のぷよの色が見える）</summary>
        public readonly Sprite? Shell;
        /// <summary>1回巻き込まれた後のヒビ入りの殻</summary>
        public readonly Sprite? ShellCrack;
        /// <summary>砕けたときに飛び散る破片に使う岩の絵</summary>
        public readonly Sprite? Chip;
        /// <summary>殻の絵がないときの、1回目のヒットで差し替えるぷよの絵（旧仕様）</summary>
        public readonly Sprite? LegacyHit;

        public RockVisuals(Sprite? shell, Sprite? shellCrack, Sprite? chip, Sprite? legacyHit)
        {
            Shell      = shell;
            ShellCrack = shellCrack;
            Chip       = chip;
            LegacyHit  = legacyHit;
        }
    }

    /// <summary>
    /// 岩化されたぷよの耐久状態。
    /// 敵スキル「岩」により既存の PuyoPiece へ後付けでアタッチされる。
    /// ぷよの上に岩の殻をかぶせ、消去に巻き込まれても耐久が残っていれば実際には消えずにヒビが入る。
    /// 耐久が尽きると殻が砕けて、ぷよは普通に消える。
    /// </summary>
    [RequireComponent(typeof(PuyoPiece))]
    public sealed class RockPuyoState : MonoBehaviour
    {
        /// <summary>殻をぷよより何段手前に描くか</summary>
        private const int ShellOrderAbovePuyo = 1;
        /// <summary>殻の大きさ（ぷよの絵に対する倍率。少し大きくしてぷよのフチを覆う）</summary>
        private const float ShellSizeRatio = 1.04f;

        private PuyoPiece?      _piece;
        private SpriteRenderer? _shell;
        private RockVisuals     _visuals;
        private int             _durabilityRemaining;

        /// <summary>これまでに巻き込まれて耐えた回数</summary>
        public int HitCount { get; private set; }

        private PuyoPiece? Piece => _piece != null ? _piece : _piece = GetComponent<PuyoPiece>();

        /// <summary>破片を描く前後関係の基準（殻があれば殻、なければぷよ本体）</summary>
        private SpriteRenderer SortRef => _shell != null ? _shell : GetComponent<SpriteRenderer>();

        /// <summary>耐久値と見た目を設定する。殻の絵があれば殻をかぶせ、なければ灰色に染める</summary>
        public void Initialize(int durability, RockVisuals visuals)
        {
            _durabilityRemaining = Mathf.Max(1, durability);
            _visuals             = visuals;

            if (visuals.Shell != null) CreateShell(visuals.Shell);
            else Piece?.SetTint(FallbackSkillVisuals.Rock);
        }

        /// <summary>
        /// 消去に巻き込まれた際に PuyoBoard から呼ぶ（判定のみ。見た目は PlayHitEffect / PlayBreakEffect で変える）。
        /// 耐久が残っていれば true を返す（＝呼び出し元は実際には消さないこと）。
        /// 耐久が尽きていれば false を返す（＝通常通り消してよい）。
        /// </summary>
        public bool OnHit()
        {
            _durabilityRemaining--;
            if (_durabilityRemaining <= 0) return false;
            HitCount++;
            return true;
        }

        /// <summary>巻き込まれて耐えたときの演出：殻にヒビが入ってガツンと揺れ、小さな欠片が飛ぶ</summary>
        public void PlayHitEffect(float cellSize, Transform effectParent)
        {            if (_shell != null)
            {
                _shell.sprite = ShellSpriteFor(HitCount, _visuals.Shell, _visuals.ShellCrack);
            }
            else if (HitCount == 1)
            {
                // 殻の絵がないときの仮表示：ヒット後の絵に差し替えるか、さらに暗い灰色にする
                if (_visuals.LegacyHit != null) Piece?.SetSprite(_visuals.LegacyHit);
                Piece?.SetTint(HitTintFor(_visuals.LegacyHit));
            }

            transform.DOShakePosition(0.25f, 0.1f * cellSize, 30).SetLink(gameObject);
            RockDebrisEffect.Spawn(transform.position, cellSize, _visuals.Chip, SortRef, effectParent, 3, 0.6f);
        }

        /// <summary>耐久が尽きて消えるときの演出：殻がガラガラ砕けて破片が飛び散る（ぷよ本体は通常どおり消える）</summary>
        public void PlayBreakEffect(float cellSize, Transform effectParent)
        {            RockDebrisEffect.Spawn(transform.position, cellSize, _visuals.Chip, SortRef, effectParent, 7, 1f);
            if (_shell != null) _shell.enabled = false;
            // 仮表示で灰色に染めていた場合は、消える瞬間だけ元の色に戻す
            Piece?.SetTint(Color.white);
        }

        /// <summary>殻の絵：まだ当たっていなければ普通の殻、1回でも当たればヒビ入り（ヒビ入りの絵がなければ普通の殻）</summary>
        public static Sprite? ShellSpriteFor(int hitCount, Sprite? shell, Sprite? crack)
            => hitCount >= 1 && crack != null ? crack : shell;

        /// <summary>
        /// 殻の絵がないとき、1回目のヒット後に本体へかける色。
        /// 専用画像があるなら岩化時の灰色の乗算を外し（白）、なければ暗い灰色にする。
        /// </summary>
        public static Color HitTintFor(Sprite? hitSprite)
            => hitSprite != null ? Color.white : FallbackSkillVisuals.RockHit;

        /// <summary>ぷよの子として殻を作り、上からかぶさるようにポンと出す</summary>
        private void CreateShell(Sprite shellSprite)
        {
            var pieceRenderer = GetComponent<SpriteRenderer>();
            var go = new GameObject("RockShell");
            go.transform.SetParent(transform, false);

            _shell = go.AddComponent<SpriteRenderer>();
            _shell.sprite         = shellSprite;
            _shell.sortingLayerID = pieceRenderer.sortingLayerID;
            _shell.sortingOrder   = pieceRenderer.sortingOrder + ShellOrderAbovePuyo;

            var pieceWidth = pieceRenderer.sprite != null ? pieceRenderer.sprite.bounds.size.x : 1f;
            var scale      = pieceWidth * ShellSizeRatio / shellSprite.bounds.size.x;
            go.transform.localScale = Vector3.one * scale * 1.35f;
            _shell.color = new Color(1f, 1f, 1f, 0f);

            DOTween.Sequence()
                .Append(go.transform.DOScale(scale, 0.14f).SetEase(Ease.InQuad))
                .Join(_shell.DOFade(1f, 0.08f))
                .Append(transform.DOPunchScale(new Vector3(0.18f, -0.18f, 0f), 0.25f, 6, 0.8f))
                .SetLink(go);
        }
    }
}
#nullable disable
