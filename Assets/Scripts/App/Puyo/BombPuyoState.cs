#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>時限爆弾の見た目に使う絵</summary>
    public readonly struct BombVisuals
    {
        /// <summary>盤面に置かれる爆弾（未設定ならお邪魔ぷよを赤く染めて仮表示）</summary>
        public readonly Sprite? Bomb;
        /// <summary>爆発の絵（未設定ならオレンジの丸で仮表示）</summary>
        public readonly Sprite? Explosion;
        /// <summary>爆弾の絵の中の導火線の先端の位置（左下0・右上1の割合）</summary>
        public readonly Vector2 FuseTip;

        public BombVisuals(Sprite? bomb, Sprite? explosion, Vector2 fuseTip)
        {
            Bomb      = bomb;
            Explosion = explosion;
            FuseTip   = fuseTip;
        }
    }

    /// <summary>
    /// 時限爆弾ぷよの状態と見た目。
    /// 敵スキル「時限爆弾」により PuyoBoard.PlaceBombAt で生成されたぷよにアタッチされる。
    /// 導火線の先で火花がパチパチ光り、爆発が近づくほど赤く速く点滅してふくらむ。
    /// 時間切れで爆発して周囲を巻き込む。連鎖に巻き込まれたら（Defuse）火花が消え、煙を出して解除される。
    /// </summary>
    [RequireComponent(typeof(PuyoPiece))]
    public sealed class BombPuyoState : MonoBehaviour
    {
        /// <summary>点滅の周期（秒）：置いた直後／爆発直前</summary>
        public const float SlowestPulseSec = 0.8f;
        public const float FastestPulseSec = 0.14f;
        /// <summary>爆弾の絵がないときの火花の位置（ぷよの真上あたり）</summary>
        public static readonly Vector2 FallbackFuseTip = new(0f, 0.45f);

        /// <summary>火花の大きさ（ぷよ何個分か）</summary>
        private const float SparkSizeInCells = 0.32f;
        /// <summary>点滅で赤くなるときの色と、ふくらむ割合</summary>
        private static readonly Color HotColor = new(1f, 0.35f, 0.3f, 1f);
        private const float PulseScale = 0.1f;

        private PuyoPiece?       _piece;
        private SpriteRenderer?  _renderer;
        private PuyoBoard?       _board;
        private BombVisuals      _visuals;
        private CancellationTokenSource? _cts;
        private CancellationToken _boardCt;
        private Transform?       _spark;
        private Color            _baseColor = Color.white;
        private Vector3          _baseScale = Vector3.one;
        private float            _fuseSeconds;
        private float            _elapsed;
        private float            _pulsePhase;
        private float            _nextSparkTime;
        private bool             _active;

        /// <summary>設置後 fuseSeconds 秒放置されると自動で爆発する</summary>
        public void Initialize(PuyoBoard board, float fuseSeconds, BombVisuals visuals, CancellationToken linkedCt)
        {
            _piece       = GetComponent<PuyoPiece>();
            _renderer    = GetComponent<SpriteRenderer>();
            _board       = board;
            _visuals     = visuals;
            _fuseSeconds = Mathf.Max(0.1f, fuseSeconds);
            _baseColor   = _renderer.color;
            _baseScale   = transform.localScale;
            _active      = true;

            CreateSpark();
            _boardCt = linkedCt;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(linkedCt);
            RunFuseAsync(_cts.Token).Forget();
        }

        /// <summary>連鎖に巻き込まれて解除された：導火線の火が消えて煙が出る（ぷよ本体は通常どおり消える）</summary>
        public void Defuse()
        {
            if (!_active) return;
            StopFuse();
            if (_board != null)
                BurnSmokeEffect.Spawn(transform.position, _board.CellSize * 0.7f, _renderer, _board.transform);
        }

        /// <summary>残り時間の割合（1=置いた直後、0=爆発）から点滅の周期を決める</summary>
        public static float PulsePeriodFor(float remainingRatio)
            => Mathf.Lerp(FastestPulseSec, SlowestPulseSec, Mathf.Clamp01(remainingRatio));

        /// <summary>導火線の先端の位置（絵の中の割合）を、爆弾の基準点から見た位置（絵の単位）に直す</summary>
        public static Vector2 FuseTipLocal(Sprite? sprite, Vector2 tipNormalized)
        {
            if (sprite == null) return FallbackFuseTip;
            var px = Vector2.Scale(tipNormalized, sprite.rect.size) - sprite.pivot;
            return px / sprite.pixelsPerUnit;
        }

        private async UniTaskVoid RunFuseAsync(CancellationToken ct)
        {
            if (await UniTask.Delay(Mathf.RoundToInt(_fuseSeconds * 1000), cancellationToken: ct)
                    .SuppressCancellationThrow()) return;
            await ExplodeAsync();
        }

#if UNITY_EDITOR
        /// <summary>デバッグ用：導火線を待たずにすぐ爆発させる</summary>
        public void Debug_ExplodeNow()
        {
            if (!_active) return;
            _cts?.Cancel();
            ExplodeAsync().Forget();
        }
#endif

        /// <summary>まだ盤面にあれば爆発させて周りを吹き飛ばす</summary>
        private async UniTask ExplodeAsync()
        {
            if (!_active || _piece == null || _board == null) return;
            // 連鎖などで既に盤面から外れていたら爆発しない
            if (_board.GetPieceAt(_piece.Cell) != _piece) return;

            var center = _piece.Cell;
            var board  = _board;
            StopFuse(); // ここで ct は止まるので、爆発は盤面側のトークンで進める
            BombExplosionEffect.Play(transform.position, board.CellSize, _visuals.Explosion, _renderer, board.transform);
            await board.ExplodeAroundAsync(center, _boardCt);
        }

        private void Update()
        {
            if (!_active || _renderer == null) return;
            // 盤面から外れた（吹き飛ばされた・消えている途中）なら見た目を動かさない
            if (_board == null || _piece == null || _board.GetPieceAt(_piece.Cell) != _piece) return;

            _elapsed += Time.deltaTime;
            var remaining = 1f - _elapsed / _fuseSeconds;

            // 爆発が近づくほど速く赤く点滅し、ふくらむ
            _pulsePhase += Time.deltaTime / PulsePeriodFor(remaining);
            var pulse = 0.5f - 0.5f * Mathf.Cos(_pulsePhase * Mathf.PI * 2f); // 0→1→0
            var heat  = pulse * Mathf.Lerp(0.35f, 1f, 1f - remaining);
            _renderer.color      = Color.Lerp(_baseColor, HotColor, heat);
            transform.localScale = _baseScale * (1f + PulseScale * heat);

            UpdateSpark();
        }

        /// <summary>導火線の先の火花：大きさと色をチラチラ変え、小さな火の粉を飛ばす</summary>
        private void UpdateSpark()
        {
            if (_spark == null || _board == null) return;
            var flicker = Random.Range(0.7f, 1.25f);
            _spark.localScale = Vector3.one * (SparkLocalSize() * flicker);

            if (Time.time < _nextSparkTime) return;
            _nextSparkTime = Time.time + Random.Range(0.04f, 0.09f);
            SpawnEmber(_spark.position, _board.CellSize);
        }

        private void SpawnEmber(Vector3 pos, float cellSize)
        {
            var go = new GameObject("BombEmber");
            go.transform.SetParent(_board!.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * (Random.Range(0.06f, 0.11f) * cellSize);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FallbackSkillVisuals.SoftCircleSprite;
            sr.color  = Random.value < 0.5f ? new Color(1f, 0.95f, 0.5f) : new Color(1f, 0.6f, 0.15f);
            if (_renderer != null)
            {
                sr.sortingLayerID = _renderer.sortingLayerID;
                sr.sortingOrder   = _renderer.sortingOrder + 3;
            }
            var dir  = new Vector3(Random.Range(-1f, 1f), Random.Range(0.2f, 1f), 0f).normalized;
            var life = Random.Range(0.18f, 0.3f);
            DOTween.Sequence()
                .Append(go.transform.DOMove(pos + dir * (Random.Range(0.2f, 0.4f) * cellSize), life).SetEase(Ease.OutQuad))
                .Join(go.transform.DOScale(0f, life).SetEase(Ease.InQuad))
                .OnComplete(() => Destroy(go))
                .SetLink(go);
        }

        private void CreateSpark()
        {
            var go = new GameObject("FuseSpark");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = FuseTipLocal(_visuals.Bomb, _visuals.FuseTip);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FallbackSkillVisuals.SoftCircleSprite;
            sr.color  = new Color(1f, 0.85f, 0.35f, 1f);
            if (_renderer != null)
            {
                sr.sortingLayerID = _renderer.sortingLayerID;
                sr.sortingOrder   = _renderer.sortingOrder + 2;
            }
            _spark = go.transform;
            _spark.localScale = Vector3.one * SparkLocalSize();
        }

        /// <summary>火花をぷよ SparkSizeInCells 個分の大きさにするための、爆弾から見た大きさ</summary>
        private float SparkLocalSize()
        {
            var parentScale = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
            var cellSize    = _board != null ? _board.CellSize * Mathf.Abs(_board.transform.lossyScale.x) : 1f;
            return SparkSizeInCells * cellSize / parentScale;
        }

        /// <summary>導火線を止める：タイマーを止め、火花を縮めて消し、点滅を元に戻す</summary>
        private void StopFuse()
        {
            _active = false;
            _cts?.Cancel();
            if (_renderer != null) _renderer.color = _baseColor;
            transform.localScale = _baseScale;
            if (_spark != null)
            {
                var spark = _spark.gameObject;
                _spark.DOScale(0f, 0.15f).SetEase(Ease.InQuad).OnComplete(() => Destroy(spark)).SetLink(spark);
                _spark = null;
            }
        }

        private void OnDestroy()
        {
            // 吹き飛ばされるなどで先に破棄された場合はここでタイマーを止める
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
#nullable disable
