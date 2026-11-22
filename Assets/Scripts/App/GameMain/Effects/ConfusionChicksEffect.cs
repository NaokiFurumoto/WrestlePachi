#nullable enable
using System.Collections.Generic;
using App.Puyo;
using DG.Tweening;
using UnityEngine;

namespace App.Effects
{
    /// <summary>
    /// 敵スキル「混乱」の演出。目を回したヒヨコが3羽飛んできて、落ちてくるペアの頭上を輪になって回る。
    /// ヒヨコの数が残り時間を表し、時間の 1/3 ごとに1羽ずつ飛び去って、いなくなったら混乱が終わる。
    /// ペアが固定されて次のペアが出るまでの間は、その場で回り続ける。Prefab は使わず実行時に作る。
    /// 混乱の時間は画面の一時停止と関係なく進むため（ConfusionSkill に合わせる）、時間は unscaled で数える。
    /// </summary>
    public sealed class ConfusionChicksEffect : MonoBehaviour
    {
        private const int ChickCount = 3;
        /// <summary>ヒヨコの大きさ（ぷよ何個分か）</summary>
        private const float ChickSizeInCells = 0.7f;
        /// <summary>輪の中心を、ペアの上のぷよの中心から何マス上に置くか</summary>
        public const float HeightAbovePairInCells = 0.95f;
        /// <summary>輪の横・縦の半径（ぷよ何個分か）。縦を短くして、頭の上を回る楕円にする</summary>
        private const float OrbitRadiusXInCells = 0.85f;
        private const float OrbitRadiusYInCells = 0.2f;
        /// <summary>1周にかかる秒数</summary>
        private const float OrbitPeriodSec = 1.1f;
        /// <summary>輪の中心がペアを追いかける速さ（大きいほどぴったりついていく）</summary>
        private const float FollowSharpness = 14f;
        /// <summary>ぷよより何段手前に描くか（輪の奥側は1段下げる）</summary>
        private const int SortingOrderAbovePuyo = 22;
        /// <summary>代わりの絵（ヒヨコの絵が未設定のとき）の色</summary>
        private static readonly Color FallbackChickColor = new(1f, 0.88f, 0.25f);

        private PuyoBoard _board = null!;
        private float _duration;
        private float _elapsed;
        private float _cellSize;
        private float _chickScale;
        private Vector3 _center;
        private bool _dismissed;
        private int _baseSortingOrder;
        private readonly List<SpriteRenderer> _chicks = new();

        // ─── 純粋な計算（テスト対象） ─────────────────────────────

        /// <summary>
        /// elapsed 秒たったときに残っているヒヨコの数。時間を total 等分し、1区切りごとに1羽減る。
        /// 終わった瞬間（elapsed &gt;= duration）に0羽になる。
        /// </summary>
        public static int ChicksRemaining(float elapsed, float duration, int total)
        {
            if (duration <= 0f) return 0;
            var ratio = Mathf.Clamp01(elapsed / duration);
            return Mathf.Clamp(Mathf.CeilToInt(total * (1f - ratio) - 1e-4f), 0, total);
        }

        /// <summary>輪の中心：ペアの上のぷよの少し上。ただし盤面のいちばん上（maxY）より上には出さない</summary>
        public static Vector3 OrbitCenter(Vector3 pairTop, float cellSize, float maxY)
            => new(pairTop.x, Mathf.Min(pairTop.y + HeightAbovePairInCells * cellSize, maxY), pairTop.z);

        // ─── 生成・終了 ─────────────────────────────────────────

        /// <summary>ヒヨコを飛ばしてくる。duration 秒たつと全羽が飛び去り、自分で消える</summary>
        public static ConfusionChicksEffect Create(PuyoBoard board, Sprite? sprite, float duration)
        {
            var go = new GameObject("ConfusionChicks");
            go.transform.SetParent(board.transform, false);
            var effect = go.AddComponent<ConfusionChicksEffect>();
            effect.Setup(board, sprite, duration);
            return effect;
        }

        /// <summary>残っているヒヨコをすぐに全部飛び去らせる（混乱が途中で終わった・上書きされたとき）</summary>
        public void Dismiss()
        {
            if (_dismissed) return;
            _dismissed = true;
            while (_chicks.Count > 0) FlyAway(_chicks.Count - 1);
            Destroy(gameObject, 0.6f);
        }

        private void Setup(PuyoBoard board, Sprite? sprite, float duration)
        {
            _board    = board;
            _duration = duration;
            _cellSize = board.CellSize;
            // ペアがいなければ、次のペアが出てくる列の上で回り始める
            var spawnTop = board.CellToWorld(new Vector2Int(PuyoBoard.SpawnColumn, board.VisibleRows - 1));
            _center = TargetCenter(OrbitCenter(spawnTop, _cellSize, spawnTop.y + _cellSize * 0.5f));

            var chickSprite = sprite != null ? sprite : FallbackSkillVisuals.SoftCircleSprite;
            _chickScale = _cellSize * ChickSizeInCells / Mathf.Max(chickSprite.bounds.size.x, 0.0001f);

            for (var i = 0; i < ChickCount; i++)
            {
                var chick = new GameObject($"Chick{i}");
                chick.transform.SetParent(transform, false);
                var sr = chick.AddComponent<SpriteRenderer>();
                sr.sprite = chickSprite;
                if (sprite == null) sr.color = FallbackChickColor;
                board.ApplyPieceSorting(sr, SortingOrderAbovePuyo);
                _baseSortingOrder = sr.sortingOrder;
                _chicks.Add(sr);

                // 上の外から1羽ずつポンと飛び込んでくる
                chick.transform.position   = _center + Vector3.up * (_cellSize * 3f);
                chick.transform.localScale = Vector3.zero;
                chick.transform.DOScale(_chickScale, 0.25f).SetDelay(i * 0.08f).SetEase(Ease.OutBack)
                     .SetUpdate(true).SetLink(chick);
            }
        }

        // ─── 毎フレーム ─────────────────────────────────────────

        private void Update()
        {
            if (_dismissed) return;
            _elapsed += Time.unscaledDeltaTime;

            // 残り時間に合わせて1羽ずつ飛び去る
            var remaining = ChicksRemaining(_elapsed, _duration, ChickCount);
            while (_chicks.Count > remaining) FlyAway(_chicks.Count - 1);
            if (_chicks.Count == 0)
            {
                _dismissed = true;
                Destroy(gameObject, 0.6f);
                return;
            }

            // 輪の中心をペアの頭上へなめらかに追いかける
            var follow = 1f - Mathf.Exp(-FollowSharpness * Time.unscaledDeltaTime);
            _center = Vector3.Lerp(_center, TargetCenter(_center), follow);

            var baseAngle = _elapsed / OrbitPeriodSec * Mathf.PI * 2f;
            for (var i = 0; i < _chicks.Count; i++)
            {
                var sr    = _chicks[i];
                var angle = baseAngle + i * Mathf.PI * 2f / ChickCount;
                var cos   = Mathf.Cos(angle);
                var sin   = Mathf.Sin(angle);
                var target = _center + new Vector3(cos * OrbitRadiusXInCells, sin * OrbitRadiusYInCells, 0f) * _cellSize;

                // 飛び込み中は輪の位置へ向かって急いで寄っていく
                var t = sr.transform;
                t.position = Vector3.Lerp(t.position, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));

                // 手前（輪の下側）を回るときは右へ、奥を回るときは左へ進むので、進む向きを向かせる
                var front = sin < 0f;
                sr.flipX = !front;
                sr.sortingOrder = _baseSortingOrder + (front ? 1 : -1);

                // 奥は少し小さく。羽ばたきで体を縦に伸び縮みさせる
                if (!DOTween.IsTweening(t))
                {
                    var depth = front ? 1f : 0.85f;
                    var flap  = 1f + Mathf.Sin(_elapsed * 22f + i * 1.7f) * 0.12f;
                    t.localScale = new Vector3(_chickScale * depth, _chickScale * depth * flap, 1f);
                }
            }
        }

        /// <summary>今の輪の中心のあるべき位置。ペアがいない間（連鎖中など）は fallback の場所で回り続ける</summary>
        private Vector3 TargetCenter(Vector3 fallback)
        {
            var pair = _board.ActivePair;
            if (pair == null) return fallback;
            var maxY = _board.CellToWorld(new Vector2Int(0, _board.VisibleRows - 1)).y + _cellSize * 0.5f;
            return OrbitCenter(pair.TopPosition, _cellSize, maxY);
        }

        /// <summary>index のヒヨコを、くるっと回りながら斜め上へ飛び去らせる</summary>
        private void FlyAway(int index)
        {
            var sr = _chicks[index];
            _chicks.RemoveAt(index);
            var t   = sr.transform;
            var dir = index % 2 == 0 ? 1f : -1f;
            t.DOKill();
            DOTween.Sequence()
                .Append(t.DOJump(t.position + new Vector3(dir * 1.6f, 2.4f, 0f) * _cellSize, _cellSize * 0.6f, 1, 0.55f))
                .Join(t.DORotate(new Vector3(0f, 0f, dir * -360f), 0.55f, RotateMode.FastBeyond360))
                .Join(sr.DOFade(0f, 0.55f).SetEase(Ease.InQuad))
                .SetUpdate(true)
                .OnComplete(() => Destroy(sr.gameObject))
                .SetLink(sr.gameObject);
        }
    }
}
#nullable disable
