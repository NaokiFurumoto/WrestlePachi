#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>
    /// 敵スキル「お邪魔落下」で、拳が列をドスンと叩く演出。
    /// 盤面の上から拳が一気に振り下ろされ、当たった瞬間に画面が揺れて衝撃の輪と土ぼこりが広がり、
    /// 拳は少し跳ね上がって消える。Prefab は使わず実行時に作る。
    /// </summary>
    public static class FistSlamEffect
    {
        /// <summary>拳の大きさ（ぷよ何個分か）・振り下ろす時間・当たってから消えるまでの時間（秒）</summary>
        private const float FistSizeInCells = 2.1f;
        private const float SlamTime        = 0.16f;
        private const float RecoverTime     = 0.25f;
        /// <summary>ぷよより何段手前に描くか</summary>
        private const int SortingOrderAbovePuyo = 24;

        /// <summary>
        /// target（叩く場所＝列のいちばん上）を拳で叩く。当たった瞬間に完了する（拳が消える動きは裏で続く）。
        /// sprite が未設定なら衝撃だけ出す。
        /// </summary>
        public static async UniTask SlamAsync(PuyoBoard board, Vector3 target, Sprite? sprite, CancellationToken ct)
        {
            var cellSize = board.CellSize;
            if (sprite != null)
            {
                var go = new GameObject("FistSlam");
                go.transform.SetParent(board.transform, true);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                board.ApplyPieceSorting(sr, SortingOrderAbovePuyo);
                var scale = cellSize * FistSizeInCells / sprite.bounds.size.x;
                go.transform.localScale = Vector3.one * scale;

                // 盤面の上の外から振り下ろす
                var topY = board.CellToWorld(new Vector2Int(0, board.VisibleRows - 1)).y + cellSize * 2.5f;
                go.transform.position = new Vector3(target.x, topY, target.z);
                // 絵のいちばん下（こぶしの先）が、列のいちばん上のぷよの頭に当たる位置まで振り下ろす
                var hitPos = target + Vector3.up * (-cellSize * 0.4f - sprite.bounds.min.y * scale);
                go.transform.DOMove(hitPos, SlamTime).SetEase(Ease.InQuad).SetLink(go);
                await UniTask.Delay(Mathf.RoundToInt(SlamTime * 1000), cancellationToken: ct);

                // 当たった：つぶれて跳ね上がり、薄くなって消える
                DOTween.Sequence()
                    .Append(go.transform.DOScale(new Vector3(scale * 1.15f, scale * 0.85f, 1f), 0.06f))
                    .Append(go.transform.DOMove(go.transform.position + Vector3.up * (cellSize * 0.6f), RecoverTime).SetEase(Ease.OutQuad))
                    .Join(go.transform.DOScale(scale, RecoverTime))
                    .Join(sr.DOFade(0f, RecoverTime).SetEase(Ease.InQuad))
                    .OnComplete(() => Object.Destroy(go))
                    .SetLink(go);
            }

            ScreenShakeController.Shake(ShakePreset.Medium);
            var ring = SpawnShockRing(board, target, cellSize);
            RockDebrisEffect.Spawn(target, cellSize, null, ring, board.transform, 0, 1.2f); // 土ぼこりだけ
        }

        /// <summary>当たった場所から広がる白い衝撃の輪（描く前後関係の基準として返す）</summary>
        private static SpriteRenderer SpawnShockRing(PuyoBoard board, Vector3 pos, float cellSize)
        {
            var go = new GameObject("FistShock");
            go.transform.SetParent(board.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * (cellSize * 0.5f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FallbackSkillVisuals.SoftCircleSprite;
            sr.color  = new Color(1f, 1f, 1f, 0.8f);
            board.ApplyPieceSorting(sr, SortingOrderAbovePuyo - 1);
            DOTween.Sequence()
                .Append(go.transform.DOScale(new Vector3(cellSize * 2.6f, cellSize * 1.2f, 1f), 0.22f).SetEase(Ease.OutQuad))
                .Join(sr.DOFade(0f, 0.22f))
                .OnComplete(() => Object.Destroy(go))
                .SetLink(go);
            return sr;
        }
    }
}
#nullable disable
