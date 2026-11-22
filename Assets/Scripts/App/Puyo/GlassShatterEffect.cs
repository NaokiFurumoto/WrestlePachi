#nullable enable
using DG.Tweening;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>
    /// ガラス玉（お邪魔ぷよ）が割れる演出。パッと白く光ったあと、とがったガラスの破片が
    /// くるくる回りながら四方へ飛び散り、キラッと光って消える。Prefab は使わず実行時に作る。
    /// </summary>
    public static class GlassShatterEffect
    {
        /// <summary>破片の数・大きさ（ぷよ何個分か）・飛んでいる時間（秒）</summary>
        private const int   ShardCount   = 9;
        private const float ShardSizeMin = 0.14f;
        private const float ShardSizeMax = 0.32f;
        private const float ShardLife    = 0.5f;
        /// <summary>ぷよより何段手前に描くか</summary>
        private const int OrderAboveRef = 12;

        private static readonly Color[] ShardColors =
        {
            new(0.92f, 0.96f, 1f, 0.95f),
            new(0.78f, 0.86f, 0.95f, 0.95f),
            new(0.66f, 0.68f, 0.74f, 0.95f), // お邪魔ぷよの銀色の地のかけら
        };

        private static Sprite? _shardSprite;

        /// <summary>pos でガラスを割る</summary>
        public static void Spawn(Vector3 pos, float cellSize, SpriteRenderer? sortRef, Transform? parent)
        {
            SpawnFlash(pos, cellSize, sortRef, parent);
            for (var i = 0; i < ShardCount; i++)
            {
                var angle = (360f * i / ShardCount + Random.Range(-18f, 18f)) * Mathf.Deg2Rad;
                SpawnShard(pos, cellSize, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), sortRef, parent);
            }
        }

        /// <summary>割れた瞬間の白い光（すぐ広がって消える）</summary>
        private static void SpawnFlash(Vector3 pos, float cellSize, SpriteRenderer? sortRef, Transform? parent)
        {
            var go = new GameObject("GlassFlash");
            go.transform.SetParent(parent, true);
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * (0.6f * cellSize);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FallbackSkillVisuals.SoftCircleSprite;
            sr.color  = new Color(0.9f, 0.96f, 1f, 0.9f);
            ApplySorting(sr, sortRef, OrderAboveRef - 1);
            DOTween.Sequence()
                .Append(go.transform.DOScale(1.5f * cellSize, 0.16f).SetEase(Ease.OutQuad))
                .Join(sr.DOFade(0f, 0.16f).SetEase(Ease.InQuad))
                .OnComplete(() => Object.Destroy(go))
                .SetLink(go);
        }

        private static void SpawnShard(Vector3 pos, float cellSize, Vector2 dir, SpriteRenderer? sortRef, Transform? parent)
        {
            var go = new GameObject("GlassShard");
            go.transform.SetParent(parent, true);
            go.transform.position = pos + (Vector3)(dir * (0.2f * cellSize));
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            // 細長かったり太かったり、形をばらつかせる
            var size = Random.Range(ShardSizeMin, ShardSizeMax) * cellSize;
            go.transform.localScale = new Vector3(size * Random.Range(0.5f, 0.9f), size, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ShardSprite;
            sr.color  = ShardColors[Random.Range(0, ShardColors.Length)];
            ApplySorting(sr, sortRef, OrderAboveRef);

            var end  = go.transform.position + new Vector3(dir.x * Random.Range(0.6f, 1.1f), dir.y * 0.5f - Random.Range(0.5f, 0.9f), 0f) * cellSize;
            var jump = Random.Range(0.25f, 0.55f) * cellSize * (dir.y > 0 ? 1.4f : 0.7f);
            var spin = Random.Range(-720f, 720f);
            DOTween.Sequence()
                .Append(go.transform.DOJump(end, jump, 1, ShardLife).SetEase(Ease.Linear))
                .Join(go.transform.DORotate(new Vector3(0f, 0f, spin), ShardLife, RotateMode.FastBeyond360))
                // 途中でキラッと光ってから消える
                .Insert(ShardLife * 0.35f, sr.DOColor(Color.white, 0.06f).SetLoops(2, LoopType.Yoyo))
                .Insert(ShardLife * 0.6f, sr.DOFade(0f, ShardLife * 0.4f))
                .OnComplete(() => Object.Destroy(go))
                .SetLink(go);
        }

        /// <summary>
        /// とがったガラスの破片の形（1ワールド単位の細長い三角形、ふちが少し明るい）。初回だけ生成して使い回す。
        /// </summary>
        private static Sprite ShardSprite
        {
            get
            {
                if (_shardSprite != null) return _shardSprite;
                const int size = 48;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                // 三角形の頂点（下の2点と上の1点、少し左右非対称にして割れたかけららしく）
                var a = new Vector2(4, 2);
                var b = new Vector2(size - 3, 10);
                var c = new Vector2(size * 0.42f, size - 2);
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var p  = new Vector2(x + 0.5f, y + 0.5f);
                    var d  = Mathf.Min(EdgeDistance(p, a, b), Mathf.Min(EdgeDistance(p, b, c), EdgeDistance(p, c, a)));
                    var inside = Inside(p, a, b, c);
                    var alpha  = inside ? Mathf.Clamp01(d / 1.2f) : 0f;
                    // ふちに近いほど明るく（ガラスの断面のきらめき）
                    var bright = inside ? Mathf.Lerp(1f, 0.82f, Mathf.Clamp01(d / 6f)) : 0f;
                    tex.SetPixel(x, y, new Color(bright, bright, bright, alpha));
                }
                tex.Apply();
                _shardSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                _shardSprite.name = "GlassShard";
                return _shardSprite;
            }
        }

        private static float EdgeDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t  = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float Cross(Vector2 o, Vector2 u, Vector2 v) => (u.x - o.x) * (v.y - o.y) - (u.y - o.y) * (v.x - o.x);
            var d1 = Cross(a, b, p);
            var d2 = Cross(b, c, p);
            var d3 = Cross(c, a, p);
            var hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            var hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNeg && hasPos);
        }

        private static void ApplySorting(SpriteRenderer sr, SpriteRenderer? sortRef, int orderAbove)
        {
            if (sortRef == null) return;
            sr.sortingLayerID = sortRef.sortingLayerID;
            sr.sortingOrder   = sortRef.sortingOrder + orderAbove;
        }
    }
}
#nullable disable
